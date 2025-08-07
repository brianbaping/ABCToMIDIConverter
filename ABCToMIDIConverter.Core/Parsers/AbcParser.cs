using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        public ParseResult Parse(string abcText)
        {
            if (string.IsNullOrWhiteSpace(abcText))
            {
                _result.AddError("Input text is empty or null");
                return _result;
            }

            try
            {
                // Tokenize
                var tokenizer = new AbcTokenizer();
                _tokens = tokenizer.Tokenize(abcText);
                _currentIndex = 0;
                _result = new ParseResult();

                // Parse
                var tune = ParseTune();
                _result.Tune = tune;

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
            while (!IsAtEnd() && CurrentToken().Type == TokenType.InformationField)
            {
                var token = CurrentToken();
                ParseInformationField(token, tune);
                Advance();

                // Skip newlines
                while (!IsAtEnd() && CurrentToken().Type == TokenType.NewLine)
                    Advance();

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
            while (!IsAtEnd())
            {
                var token = CurrentToken();

                switch (token.Type)
                {
                    case TokenType.Note:
                        // For now, we'll create a placeholder - we'll implement note parsing next
                        _result.AddWarning($"Note parsing not yet implemented: {token.Value}", token.Line, token.Column);
                        break;

                    case TokenType.Rest:
                        _result.AddWarning($"Rest parsing not yet implemented: {token.Value}", token.Line, token.Column);
                        break;

                    case TokenType.NewLine:
                    case TokenType.Comment:
                        // Skip these for now
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