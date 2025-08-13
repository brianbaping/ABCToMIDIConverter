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
        /// Converts an ABC tune to MIDI events for playback
        /// </summary>
        public MidiEventCollection ConvertToMidi(AbcTune tune)
        {
            if (tune == null)
                throw new ArgumentNullException(nameof(tune));

            // Create MIDI event collection
            var midiEventCollection = new MidiEventCollection(1, TicksPerQuarterNote);

            // Create a track (track 0)
            var track = new List<MidiEvent>();

            // Add track name
            track.Add(new TextEvent(tune.Title ?? "Converted ABC", MetaEventType.SequenceTrackName, 0));

            // Set tempo (convert BPM to microseconds per quarter note)
            int microsecondsPerQuarterNote = 60000000 / tune.Tempo;
            track.Add(new TempoEvent(microsecondsPerQuarterNote, 0));

            // Set time signature
            track.Add(new TimeSignatureEvent(
                tune.TimeSignature.Numerator,
                (byte)Math.Log2(tune.TimeSignature.Denominator),
                24, 8, 0));

            // Convert notes using timing calculator
            var calculator = new TimingCalculator(tune.TimeSignature, tune.UnitNoteLength, TicksPerQuarterNote);
            var midiNotes = ConvertToMidiNotes(tune, calculator);

            // Add MIDI events to track
            foreach (var note in midiNotes)
            {
                track.Add(new NoteOnEvent(note.StartTime, 1, note.NoteNumber, 80, 0));
                track.Add(new NoteOnEvent(note.StartTime + note.Duration, 1, note.NoteNumber, 0, 0)); // Note off with velocity 0
            }

            // Add end of track
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Count > 0 ? track[track.Count - 1].AbsoluteTime : 0));

            midiEventCollection.AddTrack(track);

            return midiEventCollection;
        }

        /// <summary>
        /// Converts ABC musical elements to MIDI notes
        /// </summary>
        private List<MidiNote> ConvertToMidiNotes(AbcTune tune, TimingCalculator calculator)
        {
            var midiNotes = new List<MidiNote>();
            long currentTime = 0;

            if (tune.Elements == null || tune.Elements.Count == 0)
            {
                // Return empty list if no elements to convert
                return midiNotes;
            }

            // Process each musical element
            foreach (var element in tune.Elements)
            {
                switch (element)
                {
                    case Note note:
                        var midiNote = ConvertNoteToMidi(note, currentTime, calculator, tune.KeySignature);
                        midiNotes.Add(midiNote);
                        currentTime += midiNote.Duration;
                        break;

                    case Rest rest:
                        // For rests, just advance the time without adding a note
                        var restDuration = calculator.GetMidiDuration(rest.Duration);
                        currentTime += restDuration;
                        break;

                    default:
                        // Skip unknown element types for now
                        break;
                }
            }

            return midiNotes;
        }

        /// <summary>
        /// Converts a single ABC note to a MIDI note
        /// </summary>
        private MidiNote ConvertNoteToMidi(Note note, long startTime, TimingCalculator calculator, KeySignature keySignature)
        {
            // Calculate MIDI note number from pitch and octave
            int midiNoteNumber = CalculateMidiNoteNumber(note, keySignature);

            // Calculate duration in MIDI ticks
            long duration = calculator.GetMidiDuration(note.Duration);

            // Set velocity based on note characteristics (could be enhanced)
            int velocity = CalculateVelocity(note);

            return new MidiNote
            {
                NoteNumber = midiNoteNumber,
                StartTime = startTime,
                Duration = duration,
                Velocity = velocity,
                Channel = 0 // Use channel 0 for now
            };
        }

        /// <summary>
        /// Calculates the MIDI note number from an ABC note
        /// </summary>
        private int CalculateMidiNoteNumber(Note note, KeySignature keySignature)
        {
            // Start with the base note in the octave (C=0, D=2, E=4, F=5, G=7, A=9, B=11)
            int semitoneOffset = note.Pitch switch
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

            // Calculate MIDI note number (middle C4 = 60)
            int midiNote = ((note.Octave + 1) * 12) + semitoneOffset;

            // Apply explicit accidentals from the note
            midiNote += GetAccidentalOffset(note.Accidental);

            // Apply key signature accidentals if no explicit accidental
            if (note.Accidental == Accidental.Natural)
            {
                // Check if this note is affected by the key signature
                midiNote += GetKeySignatureAccidentalOffset(note.Pitch, keySignature);
            }

            // Ensure the note is within valid MIDI range (0-127)
            return Math.Max(0, Math.Min(127, midiNote));
        }

        /// <summary>
        /// Gets the semitone offset for an accidental
        /// </summary>
        private int GetAccidentalOffset(Accidental accidental)
        {
            return accidental switch
            {
                Accidental.DoubleFlat => -2,
                Accidental.Flat => -1,
                Accidental.Natural => 0,
                Accidental.Sharp => 1,
                Accidental.DoubleSharp => 2,
                _ => 0
            };
        }

        /// <summary>
        /// Gets the key signature accidental offset for a note
        /// </summary>
        private int GetKeySignatureAccidentalOffset(char notePitch, KeySignature keySignature)
        {
            // This is a simplified implementation
            // In a full implementation, you'd need to consider the complete circle of fifths
            var sharps = keySignature.GetAccidentalCount();
            
            if (sharps > 0)
            {
                // Sharp keys - apply sharps in order: F#, C#, G#, D#, A#, E#, B#
                var sharpOrder = new[] { 'F', 'C', 'G', 'D', 'A', 'E', 'B' };
                for (int i = 0; i < Math.Min(sharps, sharpOrder.Length); i++)
                {
                    if (sharpOrder[i] == notePitch)
                        return 1; // Apply sharp
                }
            }
            else if (sharps < 0)
            {
                // Flat keys - apply flats in order: Bb, Eb, Ab, Db, Gb, Cb, Fb
                var flatOrder = new[] { 'B', 'E', 'A', 'D', 'G', 'C', 'F' };
                for (int i = 0; i < Math.Min(-sharps, flatOrder.Length); i++)
                {
                    if (flatOrder[i] == notePitch)
                        return -1; // Apply flat
                }
            }

            return 0; // No accidental from key signature
        }

        /// <summary>
        /// Calculates velocity based on note characteristics
        /// </summary>
        private int CalculateVelocity(Note note)
        {
            // Base velocity
            int velocity = 80;

            // Could be enhanced based on:
            // - Note duration (longer notes slightly softer?)
            // - Octave (higher notes slightly softer?)
            // - Musical context (accents, dynamics, etc.)

            // For now, use a consistent velocity with slight variation
            return Math.Max(40, Math.Min(127, velocity));
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