using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ABCToMIDIConverter.Core.Models;

namespace ABCToMIDIConverter.Core.Parsers
{
    /// <summary>
    /// Parses ABC notation into structured data
    /// </summary>
    public class AbcParser
    {
        private List<Token> _tokens = new List<Token>();
        private int _currentIndex;
        private ParseResult _result = new ParseResult();
        
        // Safety limits to prevent infinite loops and stack overflow
        private const int MAX_PARSE_ITERATIONS = 100_000;
        private const int MAX_RECURSION_DEPTH = 100;
        private int _recursionDepth = 0;
        
        // Timeout support
        private CancellationToken _cancellationToken;
        private DateTime _parseStartTime;

        public ParseResult Parse(string abcText, int timeoutSeconds = 60)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            return ParseWithCancellation(abcText, cts.Token);
        }

        public ParseResult ParseWithCancellation(string abcText, CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;
            _parseStartTime = DateTime.UtcNow;
            
            if (string.IsNullOrWhiteSpace(abcText))
            {
                _result.AddError("Input text is empty or null");
                return _result;
            }

            try
            {
                // Reset state
                _recursionDepth = 0;
                
                // Check for cancellation
                _cancellationToken.ThrowIfCancellationRequested();
                
                // Tokenize with safety measures
                var tokenizer = new AbcTokenizer();
                _tokens = tokenizer.Tokenize(abcText);
                
                // Check for cancellation after tokenization
                _cancellationToken.ThrowIfCancellationRequested();
                
                _currentIndex = 0;
                _result = new ParseResult();

                // Parse with safety measures
                var tune = ParseTune();
                
                // Final cancellation check
                _cancellationToken.ThrowIfCancellationRequested();
                
                _result.Tune = tune;

                return _result;
            }
            catch (OperationCanceledException)
            {
                var elapsed = (DateTime.UtcNow - _parseStartTime).TotalSeconds;
                _result.AddError($"Parsing operation was cancelled/timed out after {elapsed:F1} seconds. The input may be too complex or contain problematic patterns.");
                return _result;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Infinite loop") || 
                                                        (ex.Message.Contains("Too many") && !ex.Message.Contains("too large")))
            {
                _result.AddError($"Parsing failed due to safety limit: {ex.Message}");
                return _result;
            }
            catch (StackOverflowException)
            {
                _result.AddError("Parsing failed due to stack overflow. The input may be too complex or contain recursive structures.");
                return _result;
            }
            catch (OutOfMemoryException)
            {
                _result.AddError("Parsing failed due to out of memory. The input file may be too large.");
                return _result;
            }
            catch (Exception ex)
            {
                _result.AddError($"Parsing failed: {ex.Message}");
                return _result;
            }
        }

        private AbcTune ParseTune()
        {
            var tune = new AbcTune();

            // Parse header
            ParseHeader(tune);

            // Parse body
            ParseBody(tune);

            return tune;
        }

        private void ParseHeader(AbcTune tune)
        {
            int iterations = 0;
            
            while (!IsAtEnd() && CurrentToken().Type == TokenType.InformationField)
            {
                // Safety check for too many header fields
                if (++iterations > 1000) // Reasonable limit for header fields
                {
                    _result.AddError("Too many header fields. Parsing stopped to prevent infinite loop.");
                    break;
                }

                var token = CurrentToken();
                ParseInformationField(token, tune);
                Advance();

                // Skip newlines with safety check
                int newlineSkips = 0;
                while (!IsAtEnd() && CurrentToken().Type == TokenType.NewLine)
                {
                    if (++newlineSkips > 1000) // Prevent infinite newline skipping
                    {
                        _result.AddError("Too many consecutive newlines in header. Parsing stopped.");
                        break;
                    }
                    Advance();
                }

                // Break if we hit K: field (end of header)
                if (token.Value.StartsWith("K:"))
                    break;
            }

            // Set default unit note length if not specified
            if (tune.UnitNoteLength == 0.125) // Default value
            {
                tune.SetDefaultUnitNoteLength();
            }
        }

        private void ParseInformationField(Token token, AbcTune tune)
        {
            string field = token.Value;
            if (field.Length < 2 || field[1] != ':')
            {
                _result.AddError($"Invalid information field format: {field}", token.Line, token.Column);
                return;
            }

            char fieldType = field[0];
            string value = field.Substring(2).Trim();

            switch (fieldType)
            {
                case 'X':
                    if (int.TryParse(value, out int refNum))
                        tune.ReferenceNumber = refNum;
                    else
                        _result.AddError($"Invalid reference number: {value}", token.Line, token.Column);
                    break;

                case 'T':
                    tune.Title = value;
                    break;

                case 'C':
                    tune.Composer = value;
                    break;

                case 'M':
                    tune.TimeSignature = ParseTimeSignature(value, token);
                    break;

                case 'L':
                    tune.UnitNoteLength = ParseUnitNoteLength(value, token);
                    break;

                case 'K':
                    tune.KeySignature = ParseKeySignature(value, token);
                    break;

                case 'Q':
                    tune.Tempo = ParseTempo(value, token);
                    break;

                default:
                    _result.AddWarning($"Unsupported information field: {fieldType}:", token.Line, token.Column);
                    break;
            }
        }

        private TimeSignature ParseTimeSignature(string value, Token token)
        {
            var timeSignature = new TimeSignature();

            if (string.IsNullOrEmpty(value))
            {
                _result.AddError("Empty time signature", token.Line, token.Column);
                return timeSignature;
            }

            // Handle special cases
            switch (value.ToUpper())
            {
                case "C":
                    timeSignature.Type = TimeSignatureType.CommonTime;
                    timeSignature.Numerator = 4;
                    timeSignature.Denominator = 4;
                    return timeSignature;

                case "C|":
                    timeSignature.Type = TimeSignatureType.CutTime;
                    timeSignature.Numerator = 2;
                    timeSignature.Denominator = 2;
                    return timeSignature;

                case "NONE":
                    timeSignature.Type = TimeSignatureType.None;
                    return timeSignature;
            }

            // Parse fraction format (e.g., "4/4", "3/4", "6/8")
            var parts = value.Split('/');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int numerator) &&
                int.TryParse(parts[1], out int denominator))
            {
                timeSignature.Numerator = numerator;
                timeSignature.Denominator = denominator;
            }
            else
            {
                _result.AddError($"Invalid time signature format: {value}", token.Line, token.Column);
            }

            return timeSignature;
        }

        private double ParseUnitNoteLength(string value, Token token)
        {
            if (string.IsNullOrEmpty(value))
            {
                _result.AddError("Empty unit note length", token.Line, token.Column);
                return 0.125;
            }

            // Parse fraction format (e.g., "1/8", "1/4")
            var parts = value.Split('/');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int numerator) &&
                int.TryParse(parts[1], out int denominator) &&
                denominator != 0)
            {
                return (double)numerator / denominator;
            }
            else if (int.TryParse(value, out int wholeNumber))
            {
                return wholeNumber;
            }
            else
            {
                _result.AddError($"Invalid unit note length format: {value}", token.Line, token.Column);
                return 0.125;
            }
        }

        private KeySignature ParseKeySignature(string value, Token token)
        {
            var keySignature = new KeySignature();

            if (string.IsNullOrEmpty(value))
            {
                _result.AddError("Empty key signature", token.Line, token.Column);
                return keySignature;
            }

            // Simple parsing - just handle basic keys like "C", "G", "Dm", "F#"
            value = value.Trim();

            if (value.Length == 0)
            {
                return keySignature; // Default to C major
            }

            keySignature.Tonic = char.ToUpper(value[0]);

            // Check for accidental
            if (value.Length > 1)
            {
                switch (value[1])
                {
                    case '#':
                        keySignature.TonicAccidental = Accidental.Sharp;
                        break;
                    case 'b':
                        keySignature.TonicAccidental = Accidental.Flat;
                        break;
                }
            }

            // Check for minor mode
            if (value.ToLower().Contains("m") || value.ToLower().Contains("min"))
            {
                keySignature.Mode = Mode.Minor;
            }

            return keySignature;
        }

        private int ParseTempo(string value, Token token)
        {
            // Simple tempo parsing - just extract numbers
            var match = Regex.Match(value, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int tempo))
            {
                return tempo;
            }

            _result.AddWarning($"Could not parse tempo: {value}, using default 120", token.Line, token.Column);
            return 120;
        }

        private void ParseBody(AbcTune tune)
        {
            int iterations = 0;
            int lastIndex = -1;
            int stuckCounter = 0;
            
            while (!IsAtEnd())
            {
                // Periodic cancellation check
                if (iterations % 100 == 0) // Check every 100 iterations
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                }

                // Safety check for infinite loops - if index hasn't advanced
                if (_currentIndex == lastIndex)
                {
                    stuckCounter++;
                    if (stuckCounter > 3)
                    {
                        _result.AddError($"Parser stuck at token index {_currentIndex}. Skipping token: {CurrentToken().Type} - {CurrentToken().Value}", CurrentToken().Line, CurrentToken().Column);
                        Advance(); // Force advance to prevent infinite loop
                        stuckCounter = 0;
                        continue;
                    }
                }
                else
                {
                    lastIndex = _currentIndex;
                    stuckCounter = 0;
                }

                // Safety check for too many iterations
                if (++iterations > MAX_PARSE_ITERATIONS)
                {
                    _result.AddError($"Parser exceeded maximum iterations ({MAX_PARSE_ITERATIONS:N0}). Parsing stopped to prevent infinite loop.");
                    break;
                }

                var token = CurrentToken();

                switch (token.Type)
                {
                    case TokenType.Note:
                        var note = ParseNote();
                        if (note != null)
                            tune.Elements.Add(note);
                        break;

                    case TokenType.Rest:
                        var rest = ParseRest();
                        if (rest != null)
                            tune.Elements.Add(rest);
                        break;

                    case TokenType.Accidental:
                        // Accidentals are handled as part of note parsing
                        _result.AddWarning($"Standalone accidental found: {token.Value}", token.Line, token.Column);
                        break;

                    case TokenType.BarLine:
                        // For now, just skip bar lines - we could add bar line objects later
                        break;

                    // Handle ornaments
                    case TokenType.Trill:
                    case TokenType.Turn:
                    case TokenType.Mordent:
                    case TokenType.InvertedMordent:
                    case TokenType.InvertedTurn:
                    case TokenType.Fermata:
                    case TokenType.Staccato:
                    case TokenType.Accent:
                    case TokenType.Marcato:
                        var ornament = ParseOrnament();
                        if (ornament != null)
                            tune.Elements.Add(ornament);
                        break;

                    case TokenType.GraceNoteStart:
                        var graceNotes = ParseGraceNotes();
                        if (graceNotes != null)
                            tune.Elements.Add(graceNotes);
                        break;

                    case TokenType.Dynamic:
                        var dynamic = ParseDynamic();
                        if (dynamic != null)
                            tune.Elements.Add(dynamic);
                        break;

                    case TokenType.NewLine:
                    case TokenType.Comment:
                        // Skip these
                        break;

                    case TokenType.EndOfFile:
                        return;

                    default:
                        _result.AddWarning($"Unhandled token in body: {token.Type} - {token.Value}", token.Line, token.Column);
                        break;
                }

                Advance();
            }
        }

        private Note? ParseNote()
        {
            var noteToken = CurrentToken();
            if (noteToken.Type != TokenType.Note)
                return null;

            try
            {
                // Look for preceding accidental
                Accidental? accidental = null;
                if (_currentIndex > 0)
                {
                    var prevToken = _tokens[_currentIndex - 1];
                    if (prevToken.Type == TokenType.Accidental)
                    {
                        accidental = ParseAccidentalValue(prevToken.Value);
                    }
                }

                // Parse the note letter and octave
                string noteValue = noteToken.Value;
                char noteLetter = noteValue[0];
                int octave = CalculateOctave(noteValue);

                // Look for following duration
                double duration = 1.0; // Default duration
                if (_currentIndex + 1 < _tokens.Count)
                {
                    var nextToken = _tokens[_currentIndex + 1];
                    if (nextToken.Type == TokenType.Duration)
                    {
                        duration = ParseDurationValue(nextToken.Value);
                        Advance(); // Skip the duration token
                    }
                }

                var note = new Note
                {
                    Pitch = char.ToUpper(noteLetter),
                    Octave = octave,
                    Duration = duration,
                    Accidental = accidental ?? Accidental.Natural,
                    IsUppercase = char.IsUpper(noteLetter)
                };

                return note;
            }
            catch (Exception ex)
            {
                _result.AddError($"Error parsing note {noteToken.Value}: {ex.Message}", noteToken.Line, noteToken.Column);
                return null;
            }
        }

        private Rest? ParseRest()
        {
            var restToken = CurrentToken();
            if (restToken.Type != TokenType.Rest)
                return null;

            try
            {
                // Look for following duration
                double duration = 1.0; // Default duration
                if (_currentIndex + 1 < _tokens.Count)
                {
                    var nextToken = _tokens[_currentIndex + 1];
                    if (nextToken.Type == TokenType.Duration)
                    {
                        duration = ParseDurationValue(nextToken.Value);
                        Advance(); // Skip the duration token
                    }
                }

                var rest = new Rest
                {
                    Duration = duration
                };

                return rest;
            }
            catch (Exception ex)
            {
                _result.AddError($"Error parsing rest {restToken.Value}: {ex.Message}", restToken.Line, restToken.Column);
                return null;
            }
        }

        private Accidental ParseAccidentalValue(string accidentalText)
        {
            return accidentalText switch
            {
                "^" => Accidental.Sharp,
                "^^" => Accidental.DoubleSharp,
                "_" => Accidental.Flat,
                "__" => Accidental.DoubleFlat,
                "=" => Accidental.Natural,
                _ => Accidental.Natural
            };
        }

        private int CalculateOctave(string noteValue)
        {
            char noteLetter = noteValue[0];
            int octave = char.IsUpper(noteLetter) ? 4 : 5; // Middle C is C4, c is C5

            // Count octave modifiers
            for (int i = 1; i < noteValue.Length; i++)
            {
                switch (noteValue[i])
                {
                    case '\'': // Apostrophe raises octave
                        octave++;
                        break;
                    case ',': // Comma lowers octave
                        octave--;
                        break;
                }
            }

            return octave;
        }

        private double ParseDurationValue(string durationText)
        {
            if (string.IsNullOrEmpty(durationText))
                return 1.0;

            try
            {
                // Handle common duration patterns
                switch (durationText)
                {
                    case "/":
                    case "//":
                        return 0.5; // Half duration
                    case "///":
                        return 0.25; // Quarter duration
                    case "////":
                        return 0.125; // Eighth duration
                }

                // Handle fraction format (e.g., "1/2", "3/4")
                if (durationText.Contains('/'))
                {
                    var parts = durationText.Split('/');
                    if (parts.Length == 2)
                    {
                        if (string.IsNullOrEmpty(parts[0])) // e.g., "/2"
                        {
                            if (int.TryParse(parts[1], out int denominator) && denominator > 0)
                                return 1.0 / denominator;
                        }
                        else if (int.TryParse(parts[0], out int numerator) && 
                                int.TryParse(parts[1], out int denom) && denom > 0)
                        {
                            return (double)numerator / denom;
                        }
                    }
                }

                // Handle whole numbers (e.g., "2", "3")
                if (int.TryParse(durationText, out int wholeDuration))
                {
                    return wholeDuration;
                }

                _result.AddWarning($"Could not parse duration: {durationText}, using default 1.0");
                return 1.0;
            }
            catch (Exception)
            {
                _result.AddWarning($"Error parsing duration: {durationText}, using default 1.0");
                return 1.0;
            }
        }

        private Ornament? ParseOrnament()
        {
            var token = CurrentToken();

            try
            {
                switch (token.Type)
                {
                    case TokenType.Trill:
                        return new Trill();

                    case TokenType.Turn:
                        return new Turn();

                    case TokenType.InvertedTurn:
                        return new Turn { IsInverted = true };

                    case TokenType.Mordent:
                        return new Mordent();

                    case TokenType.InvertedMordent:
                        return new Mordent { IsInverted = true };

                    case TokenType.Fermata:
                        return new Articulation(OrnamentType.Fermata);

                    case TokenType.Staccato:
                        return new Articulation(OrnamentType.Staccato);

                    case TokenType.Accent:
                        return new Articulation(OrnamentType.Accent);

                    case TokenType.Marcato:
                        return new Articulation(OrnamentType.Marcato);

                    default:
                        _result.AddWarning($"Unrecognized ornament token: {token.Type}", token.Line, token.Column);
                        return null;
                }
            }
            catch (Exception ex)
            {
                _result.AddError($"Error parsing ornament: {ex.Message}", token.Line, token.Column);
                return null;
            }
        }

        private GraceNotes? ParseGraceNotes()
        {
            var startToken = CurrentToken();
            if (startToken.Type != TokenType.GraceNoteStart)
                return null;

            try
            {
                var graceNotes = new GraceNotes();
                Advance(); // Skip the opening brace

                // Parse notes within the grace note group
                int graceNoteIterations = 0;
                while (!IsAtEnd() && CurrentToken().Type != TokenType.GraceNoteEnd)
                {
                    // Safety check for too many grace notes
                    if (++graceNoteIterations > 100) // Reasonable limit for grace notes in a group
                    {
                        _result.AddError("Too many grace notes in group. Parsing stopped.", CurrentToken().Line, CurrentToken().Column);
                        break;
                    }

                    var token = CurrentToken();

                    switch (token.Type)
                    {
                        case TokenType.Note:
                            var note = ParseNote();
                            if (note != null)
                            {
                                // Grace notes are typically shorter
                                note.Duration *= 0.25; // Make them 1/4 of normal duration
                                graceNotes.Notes.Add(note);
                            }
                            break;

                        case TokenType.Accidental:
                            // Will be handled as part of note parsing
                            break;

                        case TokenType.Duration:
                            // Will be handled as part of note parsing
                            break;

                        default:
                            _result.AddWarning($"Unexpected token in grace notes: {token.Type}", token.Line, token.Column);
                            break;
                    }

                    Advance();
                }

                // Check for closing brace
                if (!IsAtEnd() && CurrentToken().Type == TokenType.GraceNoteEnd)
                {
                    // Don't advance here - let the main parser handle it
                    return graceNotes;
                }
                else
                {
                    _result.AddError("Unclosed grace note group", startToken.Line, startToken.Column);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _result.AddError($"Error parsing grace notes: {ex.Message}", startToken.Line, startToken.Column);
                return null;
            }
        }

        /// <summary>
        /// Parses a dynamic marking (volume indication)
        /// </summary>
        private Dynamics? ParseDynamic()
        {
            var token = CurrentToken();
            if (token.Type != TokenType.Dynamic)
                return null;

            try
            {
                var dynamicType = GetDynamicType(token.Value);
                
                var dynamic = new Dynamics(dynamicType, token.Value)
                {
                    Position = token.Position,
                    LineNumber = token.Line
                };

                return dynamic;
            }
            catch (Exception ex)
            {
                _result.AddError($"Error parsing dynamic '{token.Value}': {ex.Message}", token.Line, token.Column);
                return null;
            }
        }

        /// <summary>
        /// Converts dynamic text to DynamicType enum
        /// </summary>
        private DynamicType GetDynamicType(string text)
        {
            return text switch
            {
                "ppp" => DynamicType.Pianississimo,
                "pp" => DynamicType.Pianissimo,
                "p" => DynamicType.Piano,
                "mp" => DynamicType.MezzoPiano,
                "mf" => DynamicType.MezzoForte,
                "f" => DynamicType.Forte,
                "ff" => DynamicType.Fortissimo,
                "fff" => DynamicType.Fortississimo,
                "crescendo" or "cresc" => DynamicType.Crescendo,
                "diminuendo" or "dim" or "decresc" => DynamicType.Diminuendo,
                "sfz" or "sforzando" => DynamicType.Sforzando,
                "accent" => DynamicType.Accent,
                _ => DynamicType.MezzoForte // Default to mf
            };
        }

        private Token CurrentToken()
        {
            if (_currentIndex >= _tokens.Count)
                return _tokens.Last(); // Should be EndOfFile

            return _tokens[_currentIndex];
        }

        private void Advance()
        {
            if (_currentIndex < _tokens.Count - 1)
                _currentIndex++;
        }

        private bool IsAtEnd()
        {
            return _currentIndex >= _tokens.Count || CurrentToken().Type == TokenType.EndOfFile;
        }
    }
}