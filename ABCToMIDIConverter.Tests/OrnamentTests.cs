using NUnit.Framework;
using ABCToMIDIConverter.Core.Models;
using ABCToMIDIConverter.Core.Parsers;
using ABCToMIDIConverter.Core.Converters;
using System.Linq;

namespace ABCToMIDIConverter.Tests
{
    [TestFixture]
    public class OrnamentTests
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
        public void Parser_ParsesTrill_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Trill Test
M:4/4
L:1/4
K:C
CT";

            // Act
            var result = _parser.Parse(abcText);

            // Assert
            Assert.IsNotNull(result.Tune);
            Assert.AreEqual(2, result.Tune.Elements.Count);
            
            var note = result.Tune.Elements[0] as Note;
            Assert.IsNotNull(note);
            Assert.AreEqual('C', note.Pitch);
            
            var trill = result.Tune.Elements[1] as Trill;
            Assert.IsNotNull(trill);
            Assert.AreEqual(OrnamentType.Trill, trill.Type);
        }

        [Test]
        public void Parser_ParsesTurn_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Turn Test
M:4/4
L:1/4
K:C
CS";

            // Act
            var result = _parser.Parse(abcText);

            // Assert
            Assert.IsNotNull(result.Tune);
            Assert.AreEqual(2, result.Tune.Elements.Count);
            
            var note = result.Tune.Elements[0] as Note;
            Assert.IsNotNull(note);
            Assert.AreEqual('C', note.Pitch);
            
            var turn = result.Tune.Elements[1] as Turn;
            Assert.IsNotNull(turn);
            Assert.AreEqual(OrnamentType.Turn, turn.Type);
            Assert.IsFalse(turn.IsInverted);
        }

        [Test]
        public void Parser_ParsesInvertedTurn_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Inverted Turn Test
M:4/4
L:1/4
K:C
C~S";

            // Act
            var result = _parser.Parse(abcText);

            // Assert
            Assert.IsNotNull(result.Tune);
            Assert.AreEqual(2, result.Tune.Elements.Count);
            
            var note = result.Tune.Elements[0] as Note;
            Assert.IsNotNull(note);
            Assert.AreEqual('C', note.Pitch);
            
            var turn = result.Tune.Elements[1] as Turn;
            Assert.IsNotNull(turn);
            Assert.AreEqual(OrnamentType.Turn, turn.Type);
            Assert.IsTrue(turn.IsInverted);
        }

        [Test]
        public void Parser_ParsesMordent_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Mordent Test
M:4/4
L:1/4
K:C
CM";

            // Act
            var result = _parser.Parse(abcText);

            // Assert
            Assert.IsNotNull(result.Tune);
            Assert.AreEqual(2, result.Tune.Elements.Count);
            
            var note = result.Tune.Elements[0] as Note;
            Assert.IsNotNull(note);
            Assert.AreEqual('C', note.Pitch);
            
            var mordent = result.Tune.Elements[1] as Mordent;
            Assert.IsNotNull(mordent);
            Assert.AreEqual(OrnamentType.Mordent, mordent.Type);
            Assert.IsFalse(mordent.IsInverted);
        }

        [Test]
        public void Parser_ParsesInvertedMordent_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Inverted Mordent Test
M:4/4
L:1/4
K:C
C~M";

            // Act
            var result = _parser.Parse(abcText);

            // Assert
            Assert.IsNotNull(result.Tune);
            Assert.AreEqual(2, result.Tune.Elements.Count);
            
            var note = result.Tune.Elements[0] as Note;
            Assert.IsNotNull(note);
            Assert.AreEqual('C', note.Pitch);
            
            var mordent = result.Tune.Elements[1] as Mordent;
            Assert.IsNotNull(mordent);
            Assert.AreEqual(OrnamentType.Mordent, mordent.Type);
            Assert.IsTrue(mordent.IsInverted);
        }

        [Test]
        public void Parser_ParsesGraceNotes_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Grace Notes Test
M:4/4
L:1/4
K:C
{ABC}D";

            // Act
            var result = _parser.Parse(abcText);

            // Assert
            Assert.IsNotNull(result.Tune);
            Assert.AreEqual(2, result.Tune.Elements.Count);
            
            var graceNotes = result.Tune.Elements[0] as GraceNotes;
            Assert.IsNotNull(graceNotes);
            Assert.AreEqual(OrnamentType.GraceNote, graceNotes.Type);
            Assert.AreEqual(3, graceNotes.Notes.Count);
            Assert.AreEqual('A', graceNotes.Notes[0].Pitch);
            Assert.AreEqual('B', graceNotes.Notes[1].Pitch);
            Assert.AreEqual('C', graceNotes.Notes[2].Pitch);
            
            var mainNote = result.Tune.Elements[1] as Note;
            Assert.IsNotNull(mainNote);
            Assert.AreEqual('D', mainNote.Pitch);
        }

        [Test]
        public void Parser_ParsesArticulations_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Articulations Test
M:4/4
L:1/4
K:C
C. DH";

            // Act
            var result = _parser.Parse(abcText);

            // Assert
            Assert.IsNotNull(result.Tune);
            Assert.AreEqual(4, result.Tune.Elements.Count);
            
            // C note
            var note1 = result.Tune.Elements[0] as Note;
            Assert.IsNotNull(note1);
            Assert.AreEqual('C', note1.Pitch);
            
            // Staccato
            var staccato = result.Tune.Elements[1] as Articulation;
            Assert.IsNotNull(staccato);
            Assert.AreEqual(OrnamentType.Staccato, staccato.Type);
            
            // D note
            var note2 = result.Tune.Elements[2] as Note;
            Assert.IsNotNull(note2);
            Assert.AreEqual('D', note2.Pitch);
            
            // Fermata
            var fermata = result.Tune.Elements[3] as Articulation;
            Assert.IsNotNull(fermata);
            Assert.AreEqual(OrnamentType.Fermata, fermata.Type);
        }

        [Test]
        public void MidiConverter_ConvertsTrillToMidiNotes_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Trill MIDI Test
M:4/4
L:1/4
K:C
CT";
            var parseResult = _parser.Parse(abcText);
            Assert.IsNotNull(parseResult.Tune);

            // Act
            var midiEvents = _converter.ConvertToMidi(parseResult.Tune);

            // Assert
            Assert.IsNotNull(midiEvents);
            Assert.IsTrue(midiEvents.Tracks > 0);
            
            // Should have multiple note events for the trill
            var noteOnEvents = midiEvents[0].Where(e => e.CommandCode == NAudio.Midi.MidiCommandCode.NoteOn).ToList();
            Assert.IsTrue(noteOnEvents.Count > 1, "Trill should generate multiple MIDI note events");
        }

        [Test]
        public void MidiConverter_ConvertsStaccatoToMidiNotes_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Staccato MIDI Test
M:4/4
L:1/4
K:C
C.";
            var parseResult = _parser.Parse(abcText);
            Assert.IsNotNull(parseResult.Tune);

            // Act
            var midiEvents = _converter.ConvertToMidi(parseResult.Tune);

            // Assert
            Assert.IsNotNull(midiEvents);
            Assert.IsTrue(midiEvents.Tracks > 0);
            
            // Should have note events with modified characteristics for staccato
            var noteOnEvents = midiEvents[0].Where(e => e.CommandCode == NAudio.Midi.MidiCommandCode.NoteOn).ToList();
            Assert.IsTrue(noteOnEvents.Count >= 1, "Should have at least one note event");
        }

        [Test]
        public void MidiConverter_ConvertsGraceNotesToMidiNotes_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Grace Notes MIDI Test
M:4/4
L:1/4
K:C
{ABC}D";
            var parseResult = _parser.Parse(abcText);
            Assert.IsNotNull(parseResult.Tune);

            // Act
            var midiEvents = _converter.ConvertToMidi(parseResult.Tune);

            // Assert
            Assert.IsNotNull(midiEvents);
            Assert.IsTrue(midiEvents.Tracks > 0);
            
            // Should have note events for grace notes plus main note
            var noteOnEvents = midiEvents[0].Where(e => e.CommandCode == NAudio.Midi.MidiCommandCode.NoteOn).ToList();
            Assert.IsTrue(noteOnEvents.Count >= 4, "Should have grace notes plus main note");
        }

        [Test]
        public void OrnamentModels_HaveCorrectDefaults()
        {
            // Test Trill defaults
            var trill = new Trill();
            Assert.AreEqual(OrnamentType.Trill, trill.Type);
            Assert.IsTrue(trill.IsUpperTrill);
            Assert.AreEqual(1, trill.Interval);

            // Test Turn defaults
            var turn = new Turn();
            Assert.AreEqual(OrnamentType.Turn, turn.Type);
            Assert.IsFalse(turn.IsInverted);

            // Test Mordent defaults
            var mordent = new Mordent();
            Assert.AreEqual(OrnamentType.Mordent, mordent.Type);
            Assert.IsFalse(mordent.IsInverted);
            Assert.IsFalse(mordent.IsLong);

            // Test GraceNotes defaults
            var graceNotes = new GraceNotes();
            Assert.AreEqual(OrnamentType.GraceNote, graceNotes.Type);
            Assert.IsNotNull(graceNotes.Notes);
            Assert.AreEqual(0, graceNotes.Notes.Count);
            Assert.IsFalse(graceNotes.IsAcciaccatura);
            Assert.AreEqual(0.125, graceNotes.Duration);

            // Test Articulation defaults for Staccato
            var staccato = new Articulation(OrnamentType.Staccato);
            Assert.AreEqual(OrnamentType.Staccato, staccato.Type);
            Assert.AreEqual(0.5, staccato.DurationMultiplier);
            Assert.AreEqual(1.1, staccato.VelocityMultiplier);
        }
    }
}
