namespace ABCToMIDIConverter.Core.Models
{
    /// <summary>
    /// Base class for all musical elements in an ABC tune
    /// </summary>
    public abstract class MusicalElement
    {
        /// <summary>
        /// The position in the original ABC text where this element was found
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Line number in the ABC file
        /// </summary>
        public int LineNumber { get; set; }
    }
}