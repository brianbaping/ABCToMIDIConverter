using NUnit.Framework;
using ABCToMIDIConverter.Core.Parsers;
using ABCToMIDIConverter.Core.Converters;
using ABCToMIDIConverter.Core.Models;
using System.IO;

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
        public void ConvertToMidiFile_WithNullTune_ThrowsArgumentNullException()
        {
            // Arrange
            var outputPath = Path.Combine(_tempDirectory, "test.mid");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _converter.ConvertToMidiFile(null, outputPath));
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
    }
}
