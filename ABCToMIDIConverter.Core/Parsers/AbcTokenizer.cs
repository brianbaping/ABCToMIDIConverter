using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ABCToMIDIConverter.Core.Parsers
{
    /// <summary>
    /// Tokenizes ABC notation text into tokens
    /// </summary>
    public class AbcTokenizer
    {
        private string _text = string.Empty;
        private int _position;
        private int _line;
        private int _column;
        
        // Safety limits to prevent infinite loops and memory issues
        private const int MAX_TEXT_LENGTH = 10_000_000; // 10MB limit
        private const int MAX_TOKENS = 1_000_000; // Maximum tokens to prevent memory exhaustion
        private const int MAX_ITERATIONS_PER_TOKEN = 1000; // Prevent infinite loops in token parsing

        public List<Token> Tokenize(string text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            
            // Safety check for input size
            if (_text.Length > MAX_TEXT_LENGTH)
            {
                throw new InvalidOperationException($"Input text too large. Maximum size is {MAX_TEXT_LENGTH:N0} characters, but received {_text.Length:N0} characters.");
            }
            
            _position = 0;
            _line = 1;
            _column = 1;

            var tokens = new List<Token>();
            int tokenCount = 0;
            int lastPosition = -1;
            int stuckCounter = 0;
            int totalIterations = 0;
            const int MAX_TOTAL_ITERATIONS = 1_000_000; // Absolute maximum iterations

            while (_position < _text.Length)
            {
                // Safety check for total iterations
                if (++totalIterations > MAX_TOTAL_ITERATIONS)
                {
                    throw new InvalidOperationException($"Tokenizer exceeded maximum iterations ({MAX_TOTAL_ITERATIONS:N0}). Position: {_position}, Line: {_line}, Character: '{(_position < _text.Length ? _text[_position] : "EOF")}'");
                }

                // Safety check for infinite loops - if position hasn't advanced
                if (_position == lastPosition)
                {
                    stuckCounter++;
                    if (stuckCounter > 3)
                    {
                        // Log the problematic character and force advance
                        char problematicChar = _position < _text.Length ? _text[_position] : '\0';
                        
                        // Force advance to prevent infinite loop
                        _position++;
                        _column++;
                        stuckCounter = 0;
                        
                        // Create an unknown token for the problematic character
                        if (problematicChar != '\0')
                        {
                            tokens.Add(new Token(TokenType.Unknown, problematicChar.ToString(), _position - 1, _line, _column - 1));
                            tokenCount++;
                        }
                        continue;
                    }
                }
                else
                {
                    lastPosition = _position;
                    stuckCounter = 0;
                }

                // Safety check for too many tokens
                if (tokenCount >= MAX_TOKENS)
                {
                    throw new InvalidOperationException($"Too many tokens generated. Maximum is {MAX_TOKENS:N0}. This may indicate malformed input or an infinite loop.");
                }

                int positionBeforeToken = _position;
                var token = GetNextToken();
                
                // Additional safety check - ensure position advanced or token was created
                if (_position == positionBeforeToken && token == null)
                {
                    // Force advance if position didn't change and no token was created
                    _position++;
                    _column++;
                }
                
                if (token != null)
                {
                    tokens.Add(token);
                    tokenCount++;
                }
            }

            tokens.Add(new Token(TokenType.EndOfFile, "", _position, _line, _column));
            return tokens;
        }

        private Token? GetNextToken()
        {
            if (_position >= _text.Length)
                return null;

            char current = _text[_position];

            // Skip whitespace (except newlines)
            if (char.IsWhiteSpace(current) && current != '\n' && current != '\r')
            {
                SkipWhitespace();
                return null;
            }

            // Handle newlines
            if (current == '\n' || current == '\r')
            {
                return HandleNewLine();
            }

            // Handle comments
            if (current == '%')
            {
                return HandleComment();
            }

            // Handle information fields (letter followed by colon)
            if (char.IsLetter(current) && _position + 1 < _text.Length && _text[_position + 1] == ':')
            {
                return HandleInformationField();
            }

            // Handle dynamics BEFORE notes to prevent 'f' and 'p' from being parsed as notes
            if (char.IsLetter(current) && CouldStartDynamic())
            {
                var dynamicToken = TryHandleDynamic();
                if (dynamicToken != null)
                    return dynamicToken;
            }

            // Handle accidentals
            if (current == '^' || current == '_' || current == '=')
            {
                return HandleAccidental();
            }

            // Handle notes (A-G, a-g)
            if (char.IsLetter(current) && IsNoteCharacter(current))
            {
                return HandleNote();
            }

            // Handle rests
            if (current == 'z' || current == 'x' || current == 'Z')
            {
                return HandleRest();
            }

            // Handle bar lines
            if (current == '|' || current == ':')
            {
                return HandleBarLine();
            }

            // Handle chord brackets
            if (current == '[')
            {
                return CreateToken(TokenType.ChordStart, "[");
            }

            if (current == ']')
            {
                return CreateToken(TokenType.ChordEnd, "]");
            }

            // Handle grace note brackets
            if (current == '{')
            {
                return CreateToken(TokenType.GraceNoteStart, "{");
            }

            if (current == '}')
            {
                return CreateToken(TokenType.GraceNoteEnd, "}");
            }

            // Handle broken rhythm
            if (current == '<' || current == '>')
            {
                return HandleBrokenRhythm();
            }

            // Handle ties and slurs
            if (current == '-')
            {
                return CreateToken(TokenType.Tie, "-");
            }

            if (current == '(' || current == ')')
            {
                return CreateToken(TokenType.Slur, current.ToString());
            }

            // Handle articulations
            if (current == '.')
            {
                return CreateToken(TokenType.Staccato, ".");
            }

            // Handle ornaments - enhanced support
            if (current == '~')
            {
                return HandleInvertedOrnament();
            }

            if (current == 'T' && !IsPartOfNote())
            {
                return CreateToken(TokenType.Trill, "T");
            }

            if (current == 'S' && !IsPartOfNote())
            {
                return CreateToken(TokenType.Turn, "S");
            }

            if (current == 'M' && !IsPartOfNote())
            {
                return CreateToken(TokenType.Mordent, "M");
            }

            if (current == 'H' && !IsPartOfNote())
            {
                return CreateToken(TokenType.Fermata, "H");
            }

            // Handle accent and marcato (need to distinguish from broken rhythm)
            if (current == '^' && !IsPartOfAccidental())
            {
                return CreateToken(TokenType.Marcato, "^");
            }

            // Handle numbers (durations)
            if (char.IsDigit(current) || current == '/')
            {
                return HandleDuration();
            }

            // Unknown character
            return CreateToken(TokenType.Unknown, current.ToString());
        }

        private void SkipWhitespace()
        {
            int iterations = 0;
            while (_position < _text.Length &&
                   char.IsWhiteSpace(_text[_position]) &&
                   _text[_position] != '\n' &&
                   _text[_position] != '\r')
            {
                _position++;
                _column++;
                
                // Safety check to prevent infinite loops
                if (++iterations > MAX_ITERATIONS_PER_TOKEN)
                {
                    throw new InvalidOperationException($"Infinite loop detected in SkipWhitespace at position {_position}, line {_line}, column {_column}");
                }
            }
        }

        private Token HandleNewLine()
        {
            var start = _position;
            if (_text[_position] == '\r' && _position + 1 < _text.Length && _text[_position + 1] == '\n')
            {
                _position += 2; // \r\n
            }
            else
            {
                _position++; // \n or \r
            }

            var token = new Token(TokenType.NewLine, _text.Substring(start, _position - start), start, _line, _column);
            _line++;
            _column = 1;
            return token;
        }

        private Token HandleComment()
        {
            var start = _position;
            var startColumn = _column;
            int iterations = 0;

            // Skip until end of line
            while (_position < _text.Length && _text[_position] != '\n' && _text[_position] != '\r')
            {
                _position++;
                _column++;
                
                // Safety check to prevent infinite loops
                if (++iterations > MAX_ITERATIONS_PER_TOKEN)
                {
                    throw new InvalidOperationException($"Infinite loop detected in HandleComment at position {_position}, line {_line}, column {_column}");
                }
            }

            return new Token(TokenType.Comment, _text.Substring(start, _position - start), start, _line, startColumn);
        }

        private Token HandleInformationField()
        {
            var start = _position;
            var startColumn = _column;
            int iterations = 0;

            // Read field identifier (letter + colon)
            _position++;
            _column++;
            _position++; // skip colon
            _column++;

            // Read until end of line
            while (_position < _text.Length && _text[_position] != '\n' && _text[_position] != '\r')
            {
                _position++;
                _column++;
                
                // Safety check to prevent infinite loops
                if (++iterations > MAX_ITERATIONS_PER_TOKEN)
                {
                    throw new InvalidOperationException($"Infinite loop detected in HandleInformationField at position {_position}, line {_line}, column {_column}");
                }
            }

            return new Token(TokenType.InformationField, _text.Substring(start, _position - start), start, _line, startColumn);
        }

        private Token HandleAccidental()
        {
            var start = _position;
            var startColumn = _column;
            char current = _text[_position];

            _position++;
            _column++;

            // Check for double accidentals (^^, __, ==)
            if (_position < _text.Length && _text[_position] == current)
            {
                _position++;
                _column++;
            }

            return new Token(TokenType.Accidental, _text.Substring(start, _position - start), start, _line, startColumn);
        }

        private Token HandleNote()
        {
            var start = _position;
            var startColumn = _column;
            int iterations = 0;

            // Read the note letter
            _position++;
            _column++;

            // Check for octave indicators (commas and apostrophes)
            while (_position < _text.Length && (_text[_position] == ',' || _text[_position] == '\''))
            {
                _position++;
                _column++;
                
                // Safety check to prevent infinite loops
                if (++iterations > MAX_ITERATIONS_PER_TOKEN)
                {
                    throw new InvalidOperationException($"Infinite loop detected in HandleNote at position {_position}, line {_line}, column {_column}");
                }
            }

            return new Token(TokenType.Note, _text.Substring(start, _position - start), start, _line, startColumn);
        }

        private Token HandleRest()
        {
            var start = _position;
            var startColumn = _column;

            _position++;
            _column++;

            return new Token(TokenType.Rest, _text.Substring(start, _position - start), start, _line, startColumn);
        }

        private Token HandleBarLine()
        {
            var start = _position;
            var startColumn = _column;
            int iterations = 0;

            // Handle different bar line combinations: |, ||, |:, :|, ::, etc.
            while (_position < _text.Length && (_text[_position] == '|' || _text[_position] == ':'))
            {
                _position++;
                _column++;
                
                // Safety check to prevent infinite loops
                if (++iterations > MAX_ITERATIONS_PER_TOKEN)
                {
                    throw new InvalidOperationException($"Infinite loop detected in HandleBarLine at position {_position}, line {_line}, column {_column}");
                }
            }

            return new Token(TokenType.BarLine, _text.Substring(start, _position - start), start, _line, startColumn);
        }

        private Token HandleDuration()
        {
            var start = _position;
            var startColumn = _column;
            int iterations = 0;

            // Handle patterns like: 2, 1/2, 3/4, /2, //
            while (_position < _text.Length && (char.IsDigit(_text[_position]) || _text[_position] == '/'))
            {
                _position++;
                _column++;
                
                // Safety check to prevent infinite loops
                if (++iterations > MAX_ITERATIONS_PER_TOKEN)
                {
                    throw new InvalidOperationException($"Infinite loop detected in HandleDuration at position {_position}, line {_line}, column {_column}");
                }
            }

            return new Token(TokenType.Duration, _text.Substring(start, _position - start), start, _line, startColumn);
        }

        private bool IsNoteCharacter(char c)
        {
            return (c >= 'A' && c <= 'G') || (c >= 'a' && c <= 'g');
        }

        private Token HandleBrokenRhythm()
        {
            var start = _position;
            var startColumn = _column;
            char current = _text[_position];

            _position++;
            _column++;

            // Check for double broken rhythm (<<, >>)
            if (_position < _text.Length && _text[_position] == current)
            {
                _position++;
                _column++;
            }

            return new Token(TokenType.BrokenRhythm, _text.Substring(start, _position - start), start, _line, startColumn);
        }

        private bool IsPartOfNote()
        {
            // Check if current position is part of a note sequence
            // This should only return true if we're in the middle of parsing a single note
            // For example: "C'" (with octave) or "C," (with octave)
            // But NOT for separate tokens like ornaments that follow notes
            
            if (_position > 0)
            {
                char prev = _text[_position - 1];
                char current = _text[_position];
                
                // Only consider it part of a note if:
                // 1. Previous char is a note AND current is an octave modifier (', or ,)
                // 2. OR we're continuing an octave sequence
                if (IsNoteCharacter(prev) && (current == '\'' || current == ','))
                {
                    return true;
                }
                
                // Continue octave sequences
                if ((prev == '\'' && current == '\'') || (prev == ',' && current == ','))
                {
                    return true;
                }
            }
            
            return false;
        }

        private bool IsPartOfAccidental()
        {
            // Check if current position is part of an accidental sequence
            // Look back to see if we're already in an accidental pattern
            if (_position > 0)
            {
                char prev = _text[_position - 1];
                return prev == '^' || prev == '_' || prev == '=';
            }
            
            // Look ahead to see if this starts an accidental pattern
            if (_position + 1 < _text.Length)
            {
                char next = _text[_position + 1];
                return IsNoteCharacter(next);
            }
            
            return false;
        }

        private Token HandleInvertedOrnament()
        {
            var start = _position;
            var startColumn = _column;
            
            _position++; // Skip the '~'
            _column++;
            
            if (_position < _text.Length)
            {
                char next = _text[_position];
                
                // Check for inverted ornaments
                if (next == 'M')
                {
                    _position++;
                    _column++;
                    return new Token(TokenType.InvertedMordent, "~M", start, _line, startColumn);
                }
                else if (next == 'S')
                {
                    _position++;
                    _column++;
                    return new Token(TokenType.InvertedTurn, "~S", start, _line, startColumn);
                }
            }
            
            // If not a recognized inverted ornament, treat as unknown
            return new Token(TokenType.Unknown, "~", start, _line, startColumn);
        }

        /// <summary>
        /// Checks if the current position could start a dynamic marking
        /// </summary>
        private bool CouldStartDynamic()
        {
            char current = _text[_position];
            
            // Dynamics typically start with p, m, f, s, c, d
            return current == 'p' || current == 'm' || current == 'f' || 
                   current == 's' || current == 'c' || current == 'd';
        }

        /// <summary>
        /// Attempts to parse a dynamic marking
        /// </summary>
        private Token? TryHandleDynamic()
        {
            int start = _position;
            int startColumn = _column;
            var sb = new StringBuilder();
            int iterations = 0;

            // Read potential dynamic text
            while (_position < _text.Length && char.IsLetter(_text[_position]))
            {
                sb.Append(_text[_position]);
                _position++;
                _column++;
                
                // Safety check to prevent infinite loops
                if (++iterations > MAX_ITERATIONS_PER_TOKEN)
                {
                    throw new InvalidOperationException($"Infinite loop detected in TryHandleDynamic at position {_position}, line {_line}, column {_column}");
                }
            }

            string dynamicText = sb.ToString();

            // Check if it's a recognized dynamic marking
            if (IsRecognizedDynamic(dynamicText))
            {
                return new Token(TokenType.Dynamic, dynamicText, start, _line, startColumn);
            }

            // Reset position if not a dynamic
            _position = start;
            _column = startColumn;
            return null;
        }

        /// <summary>
        /// Checks if the given text is a recognized dynamic marking
        /// </summary>
        private bool IsRecognizedDynamic(string text)
        {
            return text switch
            {
                // Standard dynamics
                "ppp" or "pp" or "p" or "mp" or "mf" or "f" or "ff" or "fff" => true,
                
                // Gradual changes
                "crescendo" or "cresc" or "diminuendo" or "dim" or "decresc" => true,
                
                // Special markings
                "sfz" or "sforzando" or "accent" => true,
                
                _ => false
            };
        }

        private Token CreateToken(TokenType type, string value)
        {
            var token = new Token(type, value, _position, _line, _column);
            _position += value.Length;
            _column += value.Length;
            return token;
        }
    }
}