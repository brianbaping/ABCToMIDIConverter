using NUnit.Framework;
using ABCToMIDIConverter.Core.Models;

namespace ABCToMIDIConverter.Tests
{
    [TestFixture]
    public class ModelTests
    {
        [TestFixture]
        public class AbcTuneTests
        {
            [Test]
            public void AbcTune_DefaultValues_AreCorrect()
            {
                // Act
                var tune = new AbcTune();

                // Assert
                Assert.That(tune.Title, Is.EqualTo(string.Empty));
                Assert.That(tune.Composer, Is.EqualTo(string.Empty));
                Assert.That(tune.TimeSignature, Is.Not.Null);
                Assert.That(tune.KeySignature, Is.Not.Null);
                Assert.That(tune.Elements, Is.Not.Null);
            }

            [Test]
            public void AbcTune_WithElements_CountIsCorrect()
            {
                // Arrange
                var tune = new AbcTune();
                var note1 = new Note { Pitch = 'C', Octave = 4, Duration = 0.25 };
                var note2 = new Note { Pitch = 'D', Octave = 4, Duration = 0.25 };

                // Act
                tune.Elements.Add(note1);
                tune.Elements.Add(note2);

                // Assert
                Assert.That(tune.Elements.Count, Is.EqualTo(2));
            }
        }

        [TestFixture]
        public class TimeSignatureTests
        {
            [Test]
            public void TimeSignature_CommonTime_IsValid()
            {
                // Act
                var timeSignature = new TimeSignature { Numerator = 4, Denominator = 4 };

                // Assert
                Assert.That(timeSignature.Numerator, Is.EqualTo(4));
                Assert.That(timeSignature.Denominator, Is.EqualTo(4));
            }

            [TestCase(3, 4)]
            [TestCase(2, 4)]
            [TestCase(6, 8)]
            [TestCase(9, 8)]
            public void TimeSignature_VariousSignatures_AreValid(int numerator, int denominator)
            {
                // Act
                var timeSignature = new TimeSignature { Numerator = numerator, Denominator = denominator };

                // Assert
                Assert.That(timeSignature.Numerator, Is.EqualTo(numerator));
                Assert.That(timeSignature.Denominator, Is.EqualTo(denominator));
            }
        }

        [TestFixture]
        public class KeySignatureTests
        {
            [TestCase('C', Mode.Major)]
            [TestCase('G', Mode.Major)]
            [TestCase('F', Mode.Major)]
            [TestCase('A', Mode.Minor)]
            [TestCase('E', Mode.Minor)]
            public void KeySignature_VariousKeys_AreValid(char tonic, Mode mode)
            {
                // Act
                var keySignature = new KeySignature { Tonic = tonic, Mode = mode };

                // Assert
                Assert.That(keySignature.Tonic, Is.EqualTo(tonic));
                Assert.That(keySignature.Mode, Is.EqualTo(mode));
            }
        }

        [TestFixture]
        public class NoteTests
        {
            [Test]
            public void Note_MiddleC_IsValid()
            {
                // Act
                var note = new Note { Pitch = 'C', Octave = 4, Duration = 0.25 };

                // Assert
                Assert.That(note.Pitch, Is.EqualTo('C'));
                Assert.That(note.Octave, Is.EqualTo(4));
                Assert.That(note.Duration, Is.EqualTo(0.25));
            }

            [TestCase('A', 3, 0.5)]
            [TestCase('B', 5, 1.0)]
            [TestCase('G', 2, 0.125)]
            public void Note_VariousNotes_AreValid(char pitch, int octave, double duration)
            {
                // Act
                var note = new Note { Pitch = pitch, Octave = octave, Duration = duration };

                // Assert
                Assert.That(note.Pitch, Is.EqualTo(pitch));
                Assert.That(note.Octave, Is.EqualTo(octave));
                Assert.That(note.Duration, Is.EqualTo(duration));
            }

            [Test]
            public void Note_GetMidiNoteNumber_MiddleC_Returns60()
            {
                // Arrange
                var note = new Note { Pitch = 'C', Octave = 4 };

                // Act
                var midiNumber = note.GetMidiNoteNumber();

                // Assert
                Assert.That(midiNumber, Is.EqualTo(60));
            }
        }

        [TestFixture]
        public class RestTests
        {
            [TestCase(0.25)]
            [TestCase(0.5)]
            [TestCase(1.0)]
            [TestCase(2.0)]
            public void Rest_VariousDurations_AreValid(double duration)
            {
                // Act
                var rest = new Rest { Duration = duration };

                // Assert
                Assert.That(rest.Duration, Is.EqualTo(duration));
            }
        }
    }
}
