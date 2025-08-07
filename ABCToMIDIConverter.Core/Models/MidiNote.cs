namespace ABCToMIDIConverter.Core.Models
{
    /// <summary>
    /// Represents a MIDI note event
    /// </summary>
    public class MidiNote
    {
        /// <summary>
        /// MIDI note number (0-127, where 60 = middle C)
        /// </summary>
        public int NoteNumber { get; set; }

        /// <summary>
        /// Velocity (volume) of the note (0-127)
        /// </summary>
        public int Velocity { get; set; } = 90;

        /// <summary>
        /// Start time in MIDI ticks
        /// </summary>
        public long StartTime { get; set; }

        /// <summary>
        /// Duration in MIDI ticks
        /// </summary>
        public long Duration { get; set; }

        /// <summary>
        /// MIDI channel (0-15)
        /// </summary>
        public int Channel { get; set; } = 0;

        public override string ToString()
        {
            return $"Note {NoteNumber} (Vel:{Velocity}) at {StartTime} for {Duration} ticks";
        }
    }
}