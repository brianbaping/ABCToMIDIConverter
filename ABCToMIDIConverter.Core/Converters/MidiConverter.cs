using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Midi;
using ABCToMIDIConverter.Core.Models;

namespace ABCToMIDIConverter.Core.Converters
{
    /// <summary>
    /// Converts ABC tunes to MIDI files
    /// </summary>
    public class MidiConverter
    {
        private const int TicksPerQuarterNote = 480;

        /// <summary>
        /// Converts an ABC tune to a MIDI file
        /// </summary>
        public void ConvertToMidiFile(AbcTune tune, string outputPath)
        {
            if (tune == null)
                throw new ArgumentNullException(nameof(tune));

            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("Output path cannot be empty", nameof(outputPath));

            // Create MIDI event collection
            var midiEventCollection = new MidiEventCollection(1, TicksPerQuarterNote);

            // Create a track (track 0)
            var track = new List<MidiEvent>();

            // Add tempo event at the beginning
            var calculator = new TimingCalculator(tune.TimeSignature, tune.UnitNoteLength, tune.Tempo);
            track.Add(new TempoEvent(calculator.GetTempoInMicroseconds(), 0));

            // Add time signature event
            track.Add(new TimeSignatureEvent(
                tune.TimeSignature.Numerator,
                GetMidiTimeSignatureDenominator(tune.TimeSignature.Denominator),
                24, 8, 0));

            // Add key signature event
            var keySignatureBytes = GetKeySignatureBytes(tune.KeySignature);
            track.Add(new KeySignatureEvent(keySignatureBytes[0], keySignatureBytes[1], 0));

            // Convert musical elements to MIDI
            var midiNotes = ConvertToMidiNotes(tune, calculator);

            // Add note events
            foreach (var midiNote in midiNotes)
            {
                // Note On event (velocity > 0 means note on)
                track.Add(new NoteEvent(midiNote.StartTime, midiNote.Channel + 1, MidiCommandCode.NoteOn, midiNote.NoteNumber, midiNote.Velocity));

                // Note Off event (velocity = 0 means note off)
                track.Add(new NoteEvent(midiNote.StartTime + midiNote.Duration, midiNote.Channel + 1, MidiCommandCode.NoteOff, midiNote.NoteNumber, 0));
            }

            // Add end of track event
            var lastEventTime = track.Count > 0 ? GetLastEventTime(track) : 0;
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, lastEventTime));

            // Sort events by absolute time
            track.Sort((x, y) => x.AbsoluteTime.CompareTo(y.AbsoluteTime));

            // Convert to delta time and add to collection
            midiEventCollection.AddTrack(track);

            // Write to file
            MidiFile.Export(outputPath, midiEventCollection);
        }

        /// <summary>
        /// Converts ABC musical elements to MIDI notes
        /// </summary>
        private List<MidiNote> ConvertToMidiNotes(AbcTune tune, TimingCalculator calculator)
        {
            var midiNotes = new List<MidiNote>();
            long currentTime = 0;

            // For now, we'll create a simple scale as a placeholder
            // TODO: Parse actual musical elements from tune.Elements

            // Create a simple C major scale for testing
            var testNotes = new[] { 60, 62, 64, 65, 67, 69, 71, 72 }; // C D E F G A B C

            foreach (var noteNumber in testNotes)
            {
                var midiNote = new MidiNote
                {
                    NoteNumber = noteNumber,
                    StartTime = currentTime,
                    Duration = calculator.GetMidiDuration(1.0), // One unit note length
                    Velocity = 90,
                    Channel = 0
                };

                midiNotes.Add(midiNote);
                currentTime += midiNote.Duration;
            }

            return midiNotes;
        }

        /// <summary>
        /// Converts time signature denominator to MIDI format
        /// </summary>
        private int GetMidiTimeSignatureDenominator(int denominator)
        {
            return denominator switch
            {
                1 => 0,   // whole note
                2 => 1,   // half note
                4 => 2,   // quarter note
                8 => 3,   // eighth note
                16 => 4,  // sixteenth note
                32 => 5,  // thirty-second note
                _ => 2    // default to quarter note
            };
        }

        /// <summary>
        /// Gets key signature bytes for MIDI format
        /// </summary>
        private byte[] GetKeySignatureBytes(KeySignature keySignature)
        {
            int accidentalCount = keySignature.GetAccidentalCount();
            byte sharpsFlats = (byte)Math.Abs(accidentalCount);
            byte majorMinor = (byte)(keySignature.Mode == Mode.Minor ? 1 : 0);

            // If negative, it's flats; MIDI uses signed byte
            if (accidentalCount < 0)
                sharpsFlats = (byte)(256 - sharpsFlats);

            return new byte[] { sharpsFlats, majorMinor };
        }

        /// <summary>
        /// Gets the absolute time of the last event in the list
        /// </summary>
        private long GetLastEventTime(List<MidiEvent> events)
        {
            long maxTime = 0;
            foreach (var midiEvent in events)
            {
                if (midiEvent.AbsoluteTime > maxTime)
                    maxTime = midiEvent.AbsoluteTime;
            }
            return maxTime;
        }
    }
}