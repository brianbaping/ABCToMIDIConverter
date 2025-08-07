namespace ABCToMIDIConverter.Core.Models
{
    /// <summary>
    /// Represents a key signature in ABC notation (K: field)
    /// </summary>
    public class KeySignature
    {
        /// <summary>
        /// The tonic note (root) of the key
        /// </summary>
        public char Tonic { get; set; } = 'C';

        /// <summary>
        /// The accidental applied to the tonic
        /// </summary>
        public Accidental TonicAccidental { get; set; } = Accidental.Natural;

        /// <summary>
        /// The mode of the key
        /// </summary>
        public Mode Mode { get; set; } = Mode.Major;

        /// <summary>
        /// Gets the number of sharps (positive) or flats (negative) in this key
        /// </summary>
        public int GetAccidentalCount()
        {
            // Circle of fifths calculation
            int baseCount = char.ToUpper(Tonic) switch
            {
                'C' => 0,
                'G' => 1,
                'D' => 2,
                'A' => 3,
                'E' => 4,
                'B' => 5,
                'F' => -1,
                _ => 0
            };

            // Adjust for tonic accidental
            baseCount += (int)TonicAccidental;

            // Adjust for mode
            if (Mode == Mode.Minor)
                baseCount -= 3;

            return baseCount;
        }

        public override string ToString()
        {
            string accidental = TonicAccidental switch
            {
                Accidental.Sharp => "#",
                Accidental.Flat => "b",
                _ => ""
            };

            string mode = Mode == Mode.Minor ? "m" : "";

            return $"{Tonic}{accidental}{mode}";
        }
    }

    /// <summary>
    /// Musical modes supported in ABC notation
    /// </summary>
    public enum Mode
    {
        Major,
        Minor,
        Ionian,
        Dorian,
        Phrygian,
        Lydian,
        Mixolydian,
        Aeolian,
        Locrian
    }
}