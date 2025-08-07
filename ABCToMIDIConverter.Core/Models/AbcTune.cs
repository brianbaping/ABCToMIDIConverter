using System.Collections.Generic;

namespace ABCToMIDIConverter.Core.Models
{
    /// <summary>
    /// Represents a complete ABC tune
    /// </summary>
    public class AbcTune
    {
        /// <summary>
        /// Reference number (X: field)
        /// </summary>
        public int ReferenceNumber { get; set; }

        /// <summary>
        /// Title of the tune (T: field)
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Composer (C: field)
        /// </summary>
        public string Composer { get; set; } = string.Empty;

        /// <summary>
        /// Time signature (M: field)
        /// </summary>
        public TimeSignature TimeSignature { get; set; } = new TimeSignature();

        /// <summary>
        /// Unit note length (L: field)
        /// </summary>
        public double UnitNoteLength { get; set; } = 0.125; // 1/8 note default

        /// <summary>
        /// Key signature (K: field)
        /// </summary>
        public KeySignature KeySignature { get; set; } = new KeySignature();

        /// <summary>
        /// Tempo (Q: field)
        /// </summary>
        public int Tempo { get; set; } = 120;

        /// <summary>
        /// The musical elements (notes, rests, bar lines, etc.)
        /// </summary>
        public List<MusicalElement> Elements { get; set; } = new List<MusicalElement>();

        /// <summary>
        /// Any parsing errors encountered
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Calculate the default unit note length based on time signature
        /// </summary>
        public void SetDefaultUnitNoteLength()
        {
            double meterValue = TimeSignature.GetDecimalValue();
            UnitNoteLength = meterValue < 0.75 ? 0.0625 : 0.125; // 1/16 or 1/8
        }
    }
}