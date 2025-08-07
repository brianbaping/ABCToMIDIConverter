namespace ABCToMIDIConverter.Core.Models
{
    /// <summary>
    /// Represents a time signature in ABC notation (M: field)
    /// </summary>
    public class TimeSignature
    {
        /// <summary>
        /// The numerator of the time signature
        /// </summary>
        public int Numerator { get; set; } = 4;

        /// <summary>
        /// The denominator of the time signature
        /// </summary>
        public int Denominator { get; set; } = 4;

        /// <summary>
        /// Special time signatures
        /// </summary>
        public TimeSignatureType Type { get; set; } = TimeSignatureType.Normal;

        /// <summary>
        /// Gets the decimal value of the time signature (e.g., 4/4 = 1.0, 3/4 = 0.75)
        /// </summary>
        public double GetDecimalValue()
        {
            return (double)Numerator / Denominator;
        }

        /// <summary>
        /// Gets the beats per measure
        /// </summary>
        public double GetBeatsPerMeasure()
        {
            return Type switch
            {
                TimeSignatureType.CommonTime => 4.0,
                TimeSignatureType.CutTime => 2.0,
                TimeSignatureType.None => 0.0,
                _ => Numerator
            };
        }

        public override string ToString()
        {
            return Type switch
            {
                TimeSignatureType.CommonTime => "C",
                TimeSignatureType.CutTime => "C|",
                TimeSignatureType.None => "none",
                _ => $"{Numerator}/{Denominator}"
            };
        }
    }

    /// <summary>
    /// Special time signature types
    /// </summary>
    public enum TimeSignatureType
    {
        Normal,
        CommonTime,  // C (4/4)
        CutTime,     // C| (2/2)
        None         // Free meter
    }
}