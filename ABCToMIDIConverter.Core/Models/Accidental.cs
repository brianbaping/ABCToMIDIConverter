namespace ABCToMIDIConverter.Core.Models
{
    /// <summary>
    /// Represents the accidental symbols in ABC notation
    /// </summary>
    public enum Accidental
    {
        /// <summary>
        /// Double flat (__)
        /// </summary>
        DoubleFlat = -2,

        /// <summary>
        /// Flat (_)
        /// </summary>
        Flat = -1,

        /// <summary>
        /// Natural (=) or no accidental
        /// </summary>
        Natural = 0,

        /// <summary>
        /// Sharp (^)
        /// </summary>
        Sharp = 1,

        /// <summary>
        /// Double sharp (^^)
        /// </summary>
        DoubleSharp = 2
    }
}