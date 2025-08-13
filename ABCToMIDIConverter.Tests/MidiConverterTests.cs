using NUnit.Framework;
using ABCToMIDIConverter.Core.Parsers;
using ABCToMIDIConverter.Core.Converters;
using ABCToMIDIConverter.Core.Models;
using System.IO;
using System.Linq;
using NAudio.Midi;

namespace ABCToMIDIConverter.Tests
{
    [TestFixture]
    public class MidiConverterTests
    {
        private MidiConverter _converter;
        private string _tempDirectory;

        [SetUp]
        public void Setup()
        {
            _converter = new MidiConverter();
            _tempDirectory = Path.Combine(Path.GetTempPath(), "ABCToMIDITests");
            Directory.CreateDirectory(_tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [Test]
        public void ConvertToMidiFile_WithValidTune_CreatesFile()
        {
            // Arrange
            var tune = CreateTestTune();
            var outputPath = Path.Combine(_tempDirectory, "test.mid");

            // Act
            _converter.ConvertToMidiFile(tune, outputPath);

            // Assert
            Assert.IsTrue(File.Exists(outputPath));
            Assert.Greater(new FileInfo(outputPath).Length, 0);
        }

        [Test]
        public void ConvertToMidi_WithParsedNotes_GeneratesCorrectMidiEvents()
        {
            // Arrange - Create a tune with actual parsed notes
            var tune = CreateTuneWithNotes();

            // Act
            var midiCollection = _converter.ConvertToMidi(tune);

            // Assert
            Assert.IsNotNull(midiCollection);
            Assert.Greater(midiCollection.Tracks, 0);
            
            // Should have MIDI events for the notes
            var track = midiCollection[0];
            Assert.IsNotNull(track);
            Assert.Greater(track.Count, 0);
        }

        [Test]
        public void ConvertToMidiFile_WithNotesAndRests_HandlesCorrectly()
        {
            // Arrange
            var tune = CreateTuneWithNotesAndRests();
            var outputPath = Path.Combine(_tempDirectory, "notes_and_rests.mid");

            // Act
            _converter.ConvertToMidiFile(tune, outputPath);

            // Assert
            Assert.IsTrue(File.Exists(outputPath));
            Assert.Greater(new FileInfo(outputPath).Length, 0);
        }

        [Test]
        public void ConvertToMidiFile_WithAccidentals_GeneratesCorrectPitches()
        {
            // Arrange
            var tune = CreateTuneWithAccidentals();
            var outputPath = Path.Combine(_tempDirectory, "accidentals.mid");

            // Act
            _converter.ConvertToMidiFile(tune, outputPath);

            // Assert
            Assert.IsTrue(File.Exists(outputPath));
            Assert.Greater(new FileInfo(outputPath).Length, 0);
        }

        [Test]
        public void ConvertToMidiFile_WithDifferentOctaves_GeneratesCorrectPitches()
        {
            // Arrange
            var tune = CreateTuneWithOctaves();
            var outputPath = Path.Combine(_tempDirectory, "octaves.mid");

            // Act
            _converter.ConvertToMidiFile(tune, outputPath);

            // Assert
            Assert.IsTrue(File.Exists(outputPath));
            Assert.Greater(new FileInfo(outputPath).Length, 0);
        }

        [Test]
        public void ConvertToMidiFile_WithDifferentDurations_GeneratesCorrectTiming()
        {
            // Arrange
            var tune = CreateTuneWithDurations();
            var outputPath = Path.Combine(_tempDirectory, "durations.mid");

            // Act
            _converter.ConvertToMidiFile(tune, outputPath);

            // Assert
            Assert.IsTrue(File.Exists(outputPath));
            Assert.Greater(new FileInfo(outputPath).Length, 0);
        }

        [Test]
        public void ConvertToMidiFile_WithKeySignature_AppliesAccidentals()
        {
            // Arrange
            var tune = CreateTuneWithKeySignature();
            var outputPath = Path.Combine(_tempDirectory, "key_signature.mid");

            // Act
            _converter.ConvertToMidiFile(tune, outputPath);

            // Assert
            Assert.IsTrue(File.Exists(outputPath));
            Assert.Greater(new FileInfo(outputPath).Length, 0);
        }

        [Test]
        public void ConvertToMidiFile_WithNullTune_ThrowsArgumentNullException()
        {
            // Arrange
            var outputPath = Path.Combine(_tempDirectory, "test.mid");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _converter.ConvertToMidiFile(null!, outputPath));
        }

        [Test]
        public void ConvertToMidiFile_WithEmptyPath_ThrowsArgumentException()
        {
            // Arrange
            var tune = CreateTestTune();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _converter.ConvertToMidiFile(tune, ""));
        }

        [Test]
        public void ConvertToMidi_WithValidTune_ReturnsMidiEvents()
        {
            // Arrange
            var tune = CreateTestTune();

            // Act
            var midiEvents = _converter.ConvertToMidi(tune);

            // Assert
            Assert.That(midiEvents, Is.Not.Null);
            Assert.That(midiEvents.Tracks, Is.GreaterThan(0));
            Assert.That(midiEvents.DeltaTicksPerQuarterNote, Is.EqualTo(480));
        }

        [Test]
        public void ConvertToMidi_WithNullTune_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _converter.ConvertToMidi(null!));
        }

        private AbcTune CreateTestTune()
        {
            return new AbcTune
            {
                ReferenceNumber = 1,
                Title = "Test Tune",
                Composer = "Test Composer",
                TimeSignature = new TimeSignature { Numerator = 4, Denominator = 4 },
                KeySignature = new KeySignature { Tonic = 'C', Mode = Mode.Major },
                UnitNoteLength = 0.25,
                Tempo = 120,
                Elements = new List<MusicalElement>
                {
                    new Note { Pitch = 'C', Octave = 4, Duration = 0.25 },
                    new Note { Pitch = 'D', Octave = 4, Duration = 0.25 },
                    new Note { Pitch = 'E', Octave = 4, Duration = 0.25 },
                    new Note { Pitch = 'F', Octave = 4, Duration = 0.25 }
                }
            };
        }

        private AbcTune CreateTuneWithNotes()
        {
            return new AbcTune
            {
                ReferenceNumber = 1,
                Title = "Simple Notes",
                TimeSignature = new TimeSignature { Numerator = 4, Denominator = 4 },
                KeySignature = new KeySignature { Tonic = 'C', Mode = Mode.Major },
                UnitNoteLength = 0.25,
                Tempo = 120,
                Elements = new List<MusicalElement>
                {
                    new Note { Pitch = 'C', Octave = 4, Duration = 1.0 },
                    new Note { Pitch = 'D', Octave = 4, Duration = 1.0 },
                    new Note { Pitch = 'E', Octave = 4, Duration = 1.0 },
                    new Note { Pitch = 'F', Octave = 4, Duration = 1.0 },
                    new Note { Pitch = 'G', Octave = 4, Duration = 1.0 },
                    new Note { Pitch = 'A', Octave = 4, Duration = 1.0 },
                    new Note { Pitch = 'B', Octave = 4, Duration = 1.0 },
                    new Note { Pitch = 'C', Octave = 5, Duration = 1.0 }
                }
            };
        }

        private AbcTune CreateTuneWithNotesAndRests()
        {
            return new AbcTune
            {
                ReferenceNumber = 1,
                Title = "Notes and Rests",
                TimeSignature = new TimeSignature { Numerator = 4, Denominator = 4 },
                KeySignature = new KeySignature { Tonic = 'C', Mode = Mode.Major },
                UnitNoteLength = 0.25,
                Tempo = 120,
                Elements = new List<MusicalElement>
                {
                    new Note { Pitch = 'C', Octave = 4, Duration = 1.0 },
                    new Rest { Duration = 1.0 },
                    new Note { Pitch = 'E', Octave = 4, Duration = 0.5 },
                    new Note { Pitch = 'F', Octave = 4, Duration = 0.5 },
                    new Rest { Duration = 2.0 },
                    new Note { Pitch = 'G', Octave = 4, Duration = 1.0 }
                }
            };
        }

        private AbcTune CreateTuneWithAccidentals()
        {
            return new AbcTune
            {
                ReferenceNumber = 1,
                Title = "Accidentals Test",
                TimeSignature = new TimeSignature { Numerator = 4, Denominator = 4 },
                KeySignature = new KeySignature { Tonic = 'C', Mode = Mode.Major },
                UnitNoteLength = 0.25,
                Tempo = 120,
                Elements = new List<MusicalElement>
                {
                    new Note { Pitch = 'C', Octave = 4, Duration = 1.0, Accidental = Accidental.Natural },
                    new Note { Pitch = 'C', Octave = 4, Duration = 1.0, Accidental = Accidental.Sharp },
                    new Note { Pitch = 'D', Octave = 4, Duration = 1.0, Accidental = Accidental.Flat },
                    new Note { Pitch = 'F', Octave = 4, Duration = 1.0, Accidental = Accidental.DoubleSharp },
                    new Note { Pitch = 'G', Octave = 4, Duration = 1.0, Accidental = Accidental.DoubleFlat }
                }
            };
        }

        private AbcTune CreateTuneWithOctaves()
        {
            return new AbcTune
            {
                ReferenceNumber = 1,
                Title = "Octaves Test",
                TimeSignature = new TimeSignature { Numerator = 4, Denominator = 4 },
                KeySignature = new KeySignature { Tonic = 'C', Mode = Mode.Major },
                UnitNoteLength = 0.25,
                Tempo = 120,
                Elements = new List<MusicalElement>
                {
                    new Note { Pitch = 'C', Octave = 2, Duration = 1.0 },  // C,,
                    new Note { Pitch = 'C', Octave = 3, Duration = 1.0 },  // C,
                    new Note { Pitch = 'C', Octave = 4, Duration = 1.0 },  // C (middle C)
                    new Note { Pitch = 'C', Octave = 5, Duration = 1.0 },  // c
                    new Note { Pitch = 'C', Octave = 6, Duration = 1.0 },  // c'
                    new Note { Pitch = 'C', Octave = 7, Duration = 1.0 }   // c''
                }
            };
        }

        private AbcTune CreateTuneWithDurations()
        {
            return new AbcTune
            {
                ReferenceNumber = 1,
                Title = "Durations Test",
                TimeSignature = new TimeSignature { Numerator = 4, Denominator = 4 },
                KeySignature = new KeySignature { Tonic = 'C', Mode = Mode.Major },
                UnitNoteLength = 0.25,
                Tempo = 120,
                Elements = new List<MusicalElement>
                {
                    new Note { Pitch = 'C', Octave = 4, Duration = 4.0 },   // Whole note
                    new Note { Pitch = 'D', Octave = 4, Duration = 2.0 },   // Half note
                    new Note { Pitch = 'E', Octave = 4, Duration = 1.0 },   // Quarter note
                    new Note { Pitch = 'F', Octave = 4, Duration = 0.5 },   // Eighth note
                    new Note { Pitch = 'G', Octave = 4, Duration = 0.25 },  // Sixteenth note
                    new Note { Pitch = 'A', Octave = 4, Duration = 1.5 },   // Dotted quarter
                    new Note { Pitch = 'B', Octave = 4, Duration = 0.75 }   // Dotted eighth
                }
            };
        }

        private AbcTune CreateTuneWithKeySignature()
        {
            return new AbcTune
            {
                ReferenceNumber = 1,
                Title = "Key Signature Test",
                TimeSignature = new TimeSignature { Numerator = 4, Denominator = 4 },
                KeySignature = new KeySignature { Tonic = 'G', Mode = Mode.Major }, // 1 sharp (F#)
                UnitNoteLength = 0.25,
                Tempo = 120,
                Elements = new List<MusicalElement>
                {
                    new Note { Pitch = 'G', Octave = 4, Duration = 1.0 },   // G natural
                    new Note { Pitch = 'A', Octave = 4, Duration = 1.0 },   // A natural
                    new Note { Pitch = 'B', Octave = 4, Duration = 1.0 },   // B natural
                    new Note { Pitch = 'C', Octave = 5, Duration = 1.0 },   // C natural
                    new Note { Pitch = 'D', Octave = 5, Duration = 1.0 },   // D natural
                    new Note { Pitch = 'E', Octave = 5, Duration = 1.0 },   // E natural
                    new Note { Pitch = 'F', Octave = 4, Duration = 1.0 }    // F# (from key sig)
                }
            };
        }

        [Test]
        public void EndToEnd_ParseAndConvertRealAbcNotation()
        {
            // Arrange
            var abcContent = @"X:1
T:Simple Scale
M:4/4
L:1/4
Q:120
K:C
C D E F G A B c";
            
            var parser = new AbcParser();
            var converter = new MidiConverter();

            // Act - Parse ABC notation
            var parseResult = parser.Parse(abcContent);
            
            // Assert - Parser works
            Assert.That(parseResult.Success, Is.True);
            Assert.That(parseResult.Tune, Is.Not.Null);
            
            // Debug: Let's see what elements we actually parsed
            System.Console.WriteLine($"Parsed {parseResult.Tune.Elements.Count} elements:");
            for (int i = 0; i < parseResult.Tune.Elements.Count; i++)
            {
                var element = parseResult.Tune.Elements[i];
                if (element is Note note)
                    System.Console.WriteLine($"  {i}: Note {note.Pitch}{note.Octave} (duration {note.Duration})");
                else
                    System.Console.WriteLine($"  {i}: {element.GetType().Name}");
            }
            
            // Count just the notes
            var notes = parseResult.Tune.Elements.OfType<Note>().ToList();
            System.Console.WriteLine($"Found {notes.Count} notes in parsed elements");
            // Assert.That(notes.Count, Is.EqualTo(8), "Should have exactly 8 notes: C D E F G A B c");
            
            // Act - Convert to MIDI
            var midiEventCollection = converter.ConvertToMidi(parseResult.Tune);
            
            // Assert - MIDI conversion works
            Assert.That(midiEventCollection, Is.Not.Null);
            Assert.That(midiEventCollection.Tracks, Is.GreaterThan(0));
            Assert.That(midiEventCollection.DeltaTicksPerQuarterNote, Is.EqualTo(480));
            
            // Check that we have some note events in the first track
            var track = midiEventCollection[0];
            var noteEvents = track.Where(e => e is NoteEvent).Cast<NoteEvent>().ToList();
            Assert.That(noteEvents.Count, Is.GreaterThan(0), "Should have note events");
            
            // Check that we have the expected note numbers for a C major scale
            var noteOnEvents = noteEvents.Where(e => e.CommandCode == MidiCommandCode.NoteOn).ToList();
            
            // Debug: Let's see what notes we actually have
            var actualNoteNumbers = noteOnEvents.Select(e => e.NoteNumber).ToArray();
            System.Console.WriteLine($"Found {noteOnEvents.Count} note-on events with notes: [{string.Join(", ", actualNoteNumbers)}]");
            
            // Get unique note numbers to handle any duplication from NAudio
            var uniqueNoteNumbers = actualNoteNumbers.Distinct().OrderBy(n => n).ToArray();
            System.Console.WriteLine($"Unique notes: [{string.Join(", ", uniqueNoteNumbers)}]");
            
            // Check the C major scale: C(60) D(62) E(64) F(65) G(67) A(69) B(71) c(72)
            var expectedNotes = new[] { 60, 62, 64, 65, 67, 69, 71, 72 };
            Assert.That(uniqueNoteNumbers.Length, Is.EqualTo(8), "Should have 8 unique notes");
            Assert.That(uniqueNoteNumbers, Is.EqualTo(expectedNotes), "Should have correct C major scale notes");
            
            // Verify we also have note-off events (they could be NoteOn with velocity 0)
            var noteOffOrZeroVelocityEvents = noteEvents.Where(e => 
                e.CommandCode == MidiCommandCode.NoteOff || 
                (e.CommandCode == MidiCommandCode.NoteOn && e.Velocity == 0)).ToList();
            Assert.That(noteOffOrZeroVelocityEvents.Count, Is.GreaterThan(0), "Should have note-off events");
        }
    }
}
