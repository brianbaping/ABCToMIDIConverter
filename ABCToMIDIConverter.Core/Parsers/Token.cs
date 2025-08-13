namespace ABCToMIDIConverter.Core.Parsers
{
    /// <summary>
    /// Represents a token in ABC notation
    /// </summary>
    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; } = string.Empty;
        public int Position { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public Token(TokenType type, string value, int position, int line, int column)
        {
            Type = type;
            Value = value;
            Position = position;
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return $"{Type}: '{Value}' at {Line}:{Column}";
        }
    }

    /// <summary>
    /// Types of tokens in ABC notation
    /// </summary>
    public enum TokenType
    {
        // Information fields
        InformationField,    // X:, T:, M:, L:, K:, etc.

        // Musical elements
        Note,               // A, B, C, etc.
        Rest,               // z, x, Z
        BarLine,            // |, ||, |:, :|, etc.
        Accidental,         // ^, =, _, ^^, __

        // Durations and modifiers
        Duration,           // 1, 2, 1/2, 3/4, etc.
        BrokenRhythm,       // <, >, <<, >>

        // Structure
        ChordStart,         // [
        ChordEnd,           // ]
        GraceNoteStart,     // {
        GraceNoteEnd,       // }
        BeamBreak,          // space within notes

        // Articulations and ornaments
        Tie,                // -
        Slur,               // (,)
        Staccato,           // .
        Accent,             // >
        Trill,              // T
        Turn,               // S
        Mordent,            // M

        // Special
        NewLine,
        Whitespace,
        Comment,            // % comment
        EndOfFile,
        Unknown
    }
}