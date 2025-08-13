using NUnit.Framework;
using ABCToMIDIConverter.Core.Models;
using ABCToMIDIConverter.Core.Parsers;
using ABCToMIDIConverter.Core.Converters;
using System.IO;
using System.Linq;

namespace ABCToMIDIConverter.Tests
{
    /// <summary>
    /// Tests for dynamics (volume markings) functionality
    /// </summary>
    [TestFixture]
    public class DynamicsTests
    {
        private AbcParser _parser;
        private MidiConverter _converter;

        [SetUp]
        public void Setup()
        {
            _parser = new AbcParser();
            _converter = new MidiConverter();
        }

        [Test]
        public void Dynamics_Creation_SetsCorrectVelocities()
        {
            // Test velocity values for different dynamics
            var pp = new Dynamics(DynamicType.Pianissimo);
            var mf = new Dynamics(DynamicType.MezzoForte);
            var ff = new Dynamics(DynamicType.Fortissimo);

            Assert.AreEqual(32, pp.Velocity);
            Assert.AreEqual(80, mf.Velocity);
            Assert.AreEqual(112, ff.Velocity);
        }

        [Test]
        public void Tokenizer_RecognizesDynamics_Successfully()
        {
            var tokenizer = new AbcTokenizer();
            
            // Test individual dynamic recognition first
            var singleTokens = tokenizer.Tokenize("pp");
            var singleDynamics = singleTokens.Where(t => t.Type == TokenType.Dynamic).ToList();
            Assert.AreEqual(1, singleDynamics.Count, "Single 'pp' should be recognized");
            Assert.AreEqual("pp", singleDynamics[0].Value);

            var ffTokens = tokenizer.Tokenize("ff");
            var ffDynamics = ffTokens.Where(t => t.Type == TokenType.Dynamic).ToList();
            Assert.AreEqual(1, ffDynamics.Count, "Single 'ff' should be recognized");
            Assert.AreEqual("ff", ffDynamics[0].Value);

            // Now test the full string with spacing
            var tokens = tokenizer.Tokenize("C pp D mf E ff F");

            // Debug output - let's see what tokens we get
            var allTokens = tokens.ToList();
            foreach (var token in allTokens)
            {
                System.Console.WriteLine($"Token: {token.Type} = '{token.Value}'");
            }

            var dynamicTokens = tokens.Where(t => t.Type == TokenType.Dynamic).ToList();
            
            Assert.AreEqual(3, dynamicTokens.Count, $"Expected 3 dynamics but got {dynamicTokens.Count}");
            Assert.AreEqual("pp", dynamicTokens[0].Value);
            Assert.AreEqual("mf", dynamicTokens[1].Value);
            Assert.AreEqual("ff", dynamicTokens[2].Value);
        }

        [Test]
        public void Parser_ParsesDynamics_Successfully()
        {
            string abcNotation = @"
X:1
T:Dynamics Test
M:4/4
L:1/4
K:C
C pp D mf E ff F";

            var result = _parser.Parse(abcNotation);
            
            Assert.IsTrue(result.Success, $"Parse failed: {string.Join(", ", result.Errors)}");
            Assert.IsNotNull(result.Tune);

            var dynamicsElements = result.Tune.Elements.OfType<Dynamics>().ToList();
            Assert.AreEqual(3, dynamicsElements.Count);

            Assert.AreEqual(DynamicType.Pianissimo, dynamicsElements[0].Type);
            Assert.AreEqual("pp", dynamicsElements[0].Text);
            
            Assert.AreEqual(DynamicType.MezzoForte, dynamicsElements[1].Type);
            Assert.AreEqual("mf", dynamicsElements[1].Text);
            
            Assert.AreEqual(DynamicType.Fortissimo, dynamicsElements[2].Type);
            Assert.AreEqual("ff", dynamicsElements[2].Text);
        }

        [Test]
        public void MidiConverter_AppliesDynamics_ToVelocity()
        {
            string abcNotation = @"
X:1
T:Dynamics MIDI Test
M:4/4
L:1/4
K:C
C pp D mf E ff F";

            var result = _parser.Parse(abcNotation);
            Assert.IsTrue(result.Success, $"Parse failed: {string.Join(", ", result.Errors)}");
            Assert.IsNotNull(result.Tune);

            var tempFile = Path.GetTempFileName() + ".mid";
            try
            {
                _converter.ConvertToMidiFile(result.Tune, tempFile);
                Assert.IsTrue(File.Exists(tempFile));

                // Read the MIDI file and check note velocities
                var midiFile = new NAudio.Midi.MidiFile(tempFile, false);
                var noteEvents = midiFile.Events[0]
                    .Where(e => e is NAudio.Midi.NoteOnEvent)
                    .Cast<NAudio.Midi.NoteOnEvent>()
                    .Where(e => e.Velocity > 0)
                    .ToList();

                Assert.AreEqual(4, noteEvents.Count, "Should have 4 note-on events");

                // C note should have default velocity (before any dynamic)
                Assert.That(noteEvents[0].Velocity, Is.InRange(70, 90), "C note velocity");
                
                // D note should have pp velocity (32)
                Assert.AreEqual(32, noteEvents[1].Velocity, "D note should have pp velocity");
                
                // E note should have mf velocity (80)  
                Assert.AreEqual(80, noteEvents[2].Velocity, "E note should have mf velocity");
                
                // F note should have ff velocity (112)
                Assert.AreEqual(112, noteEvents[3].Velocity, "F note should have ff velocity");
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Test]
        public void Dynamics_ToString_FormatsCorrectly()
        {
            var pp = new Dynamics(DynamicType.Pianissimo);
            var crescendo = new Dynamics(DynamicType.Crescendo, "cresc");

            Assert.AreEqual("Pianissimo (pp) - Velocity: 32", pp.ToString());
            Assert.AreEqual("Crescendo (cresc) - Velocity: 80", crescendo.ToString());
        }
    }
}
