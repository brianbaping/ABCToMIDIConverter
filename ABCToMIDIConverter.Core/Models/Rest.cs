namespace ABCToMIDIConverter.Core.Models
{
    /// <summary>
    /// Represents a rest in ABC notation
    /// </summary>
    public class Rest
    {
        /// <summary>
        /// The duration of the rest
        /// </summary>
        public double Duration { get; set; } = 1.0;

        /// <summary>
        /// Whether this is a visible rest (z) or invisible rest (x)
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Whether this is a multi-measure rest (Z)
        /// </summary>
        public bool IsMultiMeasure { get; set; } = false;

        /// <summary>
        /// Number of measures for multi-measure rest
        /// </summary>
        public int MeasureCount { get; set; } = 1;

        public override string ToString()
        {
            if (IsMultiMeasure)
                return $"Z{(MeasureCount > 1 ? MeasureCount.ToString() : "")}";

            string symbol = IsVisible ? "z" : "x";
            return $"{symbol}{(Duration != 1.0 ? Duration.ToString() : "")}";
        }
    }
}