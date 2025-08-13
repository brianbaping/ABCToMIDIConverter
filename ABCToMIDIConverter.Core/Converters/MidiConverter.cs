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
            Ornament? pendingOrnament = null; // Track ornaments to apply to next note
            Dynamics? currentDynamic = null; // Track current dynamic for velocity calculations

            if (tune.Elements == null || tune.Elements.Count == 0)
            {
                // Return empty list if no elements to convert
                return midiNotes;
            }

            // Process each musical element
            for (int i = 0; i < tune.Elements.Count; i++)
            {
                var element = tune.Elements[i];

                switch (element)
                {
                    case Note note:
                        var midiNote = ConvertNoteToMidi(note, currentTime, calculator, tune.KeySignature);
                        
                        // Apply current dynamic if present
                        if (currentDynamic != null)
                        {
                            ApplyDynamicToMidiNote(midiNote, currentDynamic);
                        }
                        
                        // Apply any pending ornament
                        if (pendingOrnament != null)
                        {
                            ApplyOrnamentToMidiNote(midiNote, pendingOrnament, midiNotes, currentTime, calculator, tune.KeySignature);
                            pendingOrnament = null;
                        }
                        else
                        {
                            midiNotes.Add(midiNote);
                        }
                        
                        currentTime += midiNote.Duration;
                        break;

                    case Rest rest:
                        // For rests, just advance the time without adding a note
                        var restDuration = calculator.GetMidiDuration(rest.Duration);
                        currentTime += restDuration;
                        pendingOrnament = null; // Clear any pending ornament
                        break;

                    case Dynamics dynamic:
                        // Update current dynamic - it will affect all subsequent notes
                        currentDynamic = dynamic;
                        break;

                    case GraceNotes graceNotes:
                        // Convert grace notes
                        var graceNoteDuration = CalculateGraceNoteDuration(graceNotes, calculator);
                        foreach (var graceNote in graceNotes.Notes)
                        {
                            var graceMidiNote = ConvertNoteToMidi(graceNote, currentTime, calculator, tune.KeySignature);
                            graceMidiNote.Duration = graceNoteDuration;
                            graceMidiNote.Velocity = (int)(graceMidiNote.Velocity * 0.7); // Grace notes are quieter
                            
                            // Apply current dynamic to grace notes too
                            if (currentDynamic != null)
                            {
                                ApplyDynamicToMidiNote(graceMidiNote, currentDynamic);
                            }
                            
                            midiNotes.Add(graceMidiNote);
                            currentTime += graceNoteDuration;
                        }
                        break;

                    case Ornament ornament:
                        // Store ornament to apply to the next note
                        pendingOrnament = ornament;
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
        /// Applies dynamic marking to a MIDI note's velocity
        /// </summary>
        private void ApplyDynamicToMidiNote(MidiNote midiNote, Dynamics dynamic)
        {
            if (midiNote == null || dynamic == null)
                return;

            // Set velocity based on dynamic type
            midiNote.Velocity = dynamic.Velocity;

            // Apply any velocity modifiers based on dynamic type
            switch (dynamic.Type)
            {
                case DynamicType.Sforzando:
                    // Sudden accent - keep full velocity but could add articulation
                    break;
                case DynamicType.Accent:
                    // Accented note - slight increase if not already at max
                    midiNote.Velocity = Math.Min(127, midiNote.Velocity + 10);
                    break;
                case DynamicType.Crescendo:
                case DynamicType.Diminuendo:
                    // These would need context of surrounding notes for gradual change
                    // For now, just use the base velocity
                    break;
            }

            // Ensure velocity stays within MIDI range
            midiNote.Velocity = Math.Max(1, Math.Min(127, midiNote.Velocity));
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
        /// Applies an ornament to a MIDI note, potentially creating additional notes
        /// </summary>
        private void ApplyOrnamentToMidiNote(MidiNote mainNote, Ornament ornament, List<MidiNote> midiNotes, long currentTime, TimingCalculator calculator, KeySignature keySignature)
        {
            switch (ornament)
            {
                case Trill trill:
                    ApplyTrill(mainNote, trill, midiNotes, calculator, keySignature);
                    break;

                case Turn turn:
                    ApplyTurn(mainNote, turn, midiNotes, calculator, keySignature);
                    break;

                case Mordent mordent:
                    ApplyMordent(mainNote, mordent, midiNotes, calculator, keySignature);
                    break;

                case Articulation articulation:
                    ApplyArticulation(mainNote, articulation);
                    midiNotes.Add(mainNote);
                    break;

                default:
                    // For unknown ornaments, just add the note without modification
                    midiNotes.Add(mainNote);
                    break;
            }
        }

        /// <summary>
        /// Applies a trill ornament to a MIDI note
        /// </summary>
        private void ApplyTrill(MidiNote mainNote, Trill trill, List<MidiNote> midiNotes, TimingCalculator calculator, KeySignature keySignature)
        {
            // Create alternating notes for the trill
            long trillNoteDuration = mainNote.Duration / 8; // Each trill note is 1/8 of the main note
            int alternateNote = mainNote.NoteNumber + (trill.IsUpperTrill ? trill.Interval : -trill.Interval);

            long currentTime = mainNote.StartTime;
            bool useMainNote = true;

            // Create 8 alternating notes for the trill
            for (int i = 0; i < 8; i++)
            {
                var trillNote = new MidiNote
                {
                    NoteNumber = useMainNote ? mainNote.NoteNumber : alternateNote,
                    StartTime = currentTime,
                    Duration = trillNoteDuration,
                    Velocity = (int)(mainNote.Velocity * 0.9), // Slightly softer than main note
                    Channel = mainNote.Channel
                };

                midiNotes.Add(trillNote);
                currentTime += trillNoteDuration;
                useMainNote = !useMainNote;
            }
        }

        /// <summary>
        /// Applies a turn ornament to a MIDI note
        /// </summary>
        private void ApplyTurn(MidiNote mainNote, Turn turn, List<MidiNote> midiNotes, TimingCalculator calculator, KeySignature keySignature)
        {
            // A turn is typically: upper neighbor, main note, lower neighbor, main note
            long turnNoteDuration = mainNote.Duration / 4;
            var turnNotes = new int[4];

            if (turn.IsInverted)
            {
                // Inverted turn: lower, main, upper, main
                turnNotes[0] = mainNote.NoteNumber - 2; // Lower neighbor
                turnNotes[1] = mainNote.NoteNumber;     // Main note
                turnNotes[2] = mainNote.NoteNumber + 2; // Upper neighbor
                turnNotes[3] = mainNote.NoteNumber;     // Main note
            }
            else
            {
                // Normal turn: upper, main, lower, main
                turnNotes[0] = mainNote.NoteNumber + 2; // Upper neighbor
                turnNotes[1] = mainNote.NoteNumber;     // Main note
                turnNotes[2] = mainNote.NoteNumber - 2; // Lower neighbor
                turnNotes[3] = mainNote.NoteNumber;     // Main note
            }

            long currentTime = mainNote.StartTime;
            for (int i = 0; i < 4; i++)
            {
                var turnNote = new MidiNote
                {
                    NoteNumber = turnNotes[i],
                    StartTime = currentTime,
                    Duration = turnNoteDuration,
                    Velocity = (int)(mainNote.Velocity * 0.8),
                    Channel = mainNote.Channel
                };

                midiNotes.Add(turnNote);
                currentTime += turnNoteDuration;
            }
        }

        /// <summary>
        /// Applies a mordent ornament to a MIDI note
        /// </summary>
        private void ApplyMordent(MidiNote mainNote, Mordent mordent, List<MidiNote> midiNotes, TimingCalculator calculator, KeySignature keySignature)
        {
            // A mordent is a quick alternation: main note, neighbor, main note
            long mordentNoteDuration = mainNote.Duration / 3;
            int neighborNote = mainNote.NoteNumber + (mordent.IsInverted ? -2 : 2);

            var notes = new[]
            {
                mainNote.NoteNumber,  // Main note
                neighborNote,         // Neighbor note
                mainNote.NoteNumber   // Main note again
            };

            long currentTime = mainNote.StartTime;
            for (int i = 0; i < 3; i++)
            {
                var mordentNote = new MidiNote
                {
                    NoteNumber = notes[i],
                    StartTime = currentTime,
                    Duration = mordentNoteDuration,
                    Velocity = (int)(mainNote.Velocity * (i == 1 ? 0.7 : 0.9)), // Neighbor note softer
                    Channel = mainNote.Channel
                };

                midiNotes.Add(mordentNote);
                currentTime += mordentNoteDuration;
            }
        }

        /// <summary>
        /// Applies articulation effects to a MIDI note
        /// </summary>
        private void ApplyArticulation(MidiNote note, Articulation articulation)
        {
            // Modify duration and velocity based on articulation type
            note.Duration = (long)(note.Duration * articulation.DurationMultiplier);
            note.Velocity = (int)(note.Velocity * articulation.VelocityMultiplier);

            // Ensure velocity stays within MIDI range
            note.Velocity = Math.Max(1, Math.Min(127, note.Velocity));
        }

        /// <summary>
        /// Calculates the duration for grace notes
        /// </summary>
        private long CalculateGraceNoteDuration(GraceNotes graceNotes, TimingCalculator calculator)
        {
            // Grace notes typically take a small fraction of a beat
            return calculator.GetMidiDuration(graceNotes.Duration);
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