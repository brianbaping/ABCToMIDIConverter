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

        public List<Token> Tokenize(string text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _position = 0;
            _line = 1;
            _column = 1;

            var tokens = new List<Token>();

            while (_position < _text.Length)
            {
                var token = GetNextToken();
                if (token != null)
                {
                    tokens.Add(token);
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
            while (_position < _text.Length &&
                   char.IsWhiteSpace(_text[_position]) &&
                   _text[_position] != '\n' &&
                   _text[_position] != '\r')
            {
                _position++;
                _column++;
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

            // Skip until end of line
            while (_position < _text.Length && _text[_position] != '\n' && _text[_position] != '\r')
            {
                _position++;
                _column++;
            }

            return new Token(TokenType.Comment, _text.Substring(start, _position - start), start, _line, startColumn);
        }

        private Token HandleInformationField()
        {
            var start = _position;
            var startColumn = _column;

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

            // Read the note letter
            _position++;
            _column++;

            // Check for octave indicators (commas and apostrophes)
            while (_position < _text.Length && (_text[_position] == ',' || _text[_position] == '\''))
            {
                _position++;
                _column++;
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

            // Handle different bar line combinations: |, ||, |:, :|, ::, etc.
            while (_position < _text.Length && (_text[_position] == '|' || _text[_position] == ':'))
            {
                _position++;
                _column++;
            }

            return new Token(TokenType.BarLine, _text.Substring(start, _position - start), start, _line, startColumn);
        }

        private Token HandleDuration()
        {
            var start = _position;
            var startColumn = _column;

            // Handle patterns like: 2, 1/2, 3/4, /2, //
            while (_position < _text.Length && (char.IsDigit(_text[_position]) || _text[_position] == '/'))
            {
                _position++;
                _column++;
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

        private Token CreateToken(TokenType type, string value)
        {
            var token = new Token(type, value, _position, _line, _column);
            _position += value.Length;
            _column += value.Length;
            return token;
        }
    }
}