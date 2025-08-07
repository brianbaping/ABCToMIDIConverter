namespace ABCToMIDIConverter.Core.Models
{
    /// <summary>
    /// Represents a musical note in ABC notation
    /// </summary>
    public class Note
    {
        /// <summary>
        /// The pitch letter (A, B, C, D, E, F, G)
        /// </summary>
        public char Pitch { get; set; }

        /// <summary>
        /// The octave indicator (0-9, where 4 is middle octave)
        /// </summary>
        public int Octave { get; set; } = 4;

        /// <summary>
        /// The accidental applied to this note
        /// </summary>
        public Accidental Accidental { get; set; } = Accidental.Natural;

        /// <summary>
        /// The duration of the note (1.0 = whole note, 0.5 = half note, etc.)
        /// </summary>
        public double Duration { get; set; } = 1.0;

        /// <summary>
        /// Whether this note is uppercase (lower octave) or lowercase (higher octave) in ABC
        /// </summary>
        public bool IsUppercase { get; set; }

        /// <summary>
        /// Gets the MIDI note number for this note
        /// </summary>
        public int GetMidiNoteNumber()
        {
            // Middle C (C4) = MIDI note 60
            int baseNote = 60; // C4

            // Adjust for the pitch letter
            int pitchOffset = char.ToUpper(Pitch) switch
            {
                'C' => 0,
                'D' => 2,
                'E' => 4,
                'F' => 5,
                'G' => 7,
                'A' => 9,
                'B' => 11,
                _ => 0
            };

            // Adjust for octave
            int octaveOffset = (Octave - 4) * 12;

            // Adjust for accidental
            int accidentalOffset = Accidental switch
            {
                Accidental.DoubleFlat => -2,
                Accidental.Flat => -1,
                Accidental.Natural => 0,
                Accidental.Sharp => 1,
                Accidental.DoubleSharp => 2,
                _ => 0
            };

            return baseNote + pitchOffset + octaveOffset + accidentalOffset;
        }

        public override string ToString()
        {
            string accidentalStr = Accidental switch
            {
                Accidental.DoubleFlat => "__",
                Accidental.Flat => "_",
                Accidental.Natural => "",
                Accidental.Sharp => "^",
                Accidental.DoubleSharp => "^^",
                _ => ""
            };

            return $"{accidentalStr}{Pitch}{Duration}";
        }
    }
}