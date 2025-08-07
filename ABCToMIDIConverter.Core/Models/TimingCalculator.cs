using System;

namespace ABCToMIDIConverter.Core.Models
{
    /// <summary>
    /// Calculates MIDI timing from ABC notation
    /// </summary>
    public class TimingCalculator
    {
        private const int TicksPerQuarterNote = 480; // Standard MIDI resolution

        public TimeSignature TimeSignature { get; }
        public double UnitNoteLength { get; }
        public int Tempo { get; }

        public TimingCalculator(TimeSignature timeSignature, double unitNoteLength, int tempo = 120)
        {
            TimeSignature = timeSignature ?? new TimeSignature();
            UnitNoteLength = unitNoteLength;
            Tempo = tempo;
        }

        /// <summary>
        /// Converts ABC duration to MIDI ticks
        /// </summary>
        /// <param name="abcDuration">Duration in ABC notation (e.g., 1.0, 0.5, 2.0)</param>
        /// <returns>Duration in MIDI ticks</returns>
        public long GetMidiDuration(double abcDuration)
        {
            // ABC duration is relative to unit note length
            // Convert to quarter notes, then to ticks
            double durationInQuarterNotes = (abcDuration * UnitNoteLength) / 0.25;
            return (long)(durationInQuarterNotes * TicksPerQuarterNote);
        }

        /// <summary>
        /// Gets the number of ticks per measure
        /// </summary>
        public long GetTicksPerMeasure()
        {
            double beatsPerMeasure = TimeSignature.GetBeatsPerMeasure();
            double beatValue = 4.0 / TimeSignature.Denominator; // Beat note value in quarter notes
            return (long)(beatsPerMeasure * beatValue * TicksPerQuarterNote);
        }

        /// <summary>
        /// Gets the current tempo in microseconds per quarter note
        /// </summary>
        public int GetTempoInMicroseconds()
        {
            return 60000000 / Tempo; // Convert BPM to microseconds per quarter note
        }
    }
}