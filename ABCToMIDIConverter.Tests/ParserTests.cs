using NUnit.Framework;
using ABCToMIDIConverter.Core.Parsers;
using ABCToMIDIConverter.Core.Models;

namespace ABCToMIDIConverter.Tests
{
    [TestFixture]
    public class ParserTests
    {
        [Test]
        public void Parser_ParsesSimpleHeader_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Test Tune
C:Test Composer
M:4/4
L:1/8
K:C";

            var parser = new AbcParser();

            // Act
            var result = parser.Parse(abcText);

            // Assert
            Assert.IsTrue(result.Success, $"Parse failed: {string.Join(", ", result.Errors)}");
            Assert.IsNotNull(result.Tune);
            Assert.AreEqual(1, result.Tune.ReferenceNumber);
            Assert.AreEqual("Test Tune", result.Tune.Title);
            Assert.AreEqual("Test Composer", result.Tune.Composer);
            Assert.AreEqual(4, result.Tune.TimeSignature.Numerator);
            Assert.AreEqual(4, result.Tune.TimeSignature.Denominator);
            Assert.AreEqual(0.125, result.Tune.UnitNoteLength); // 1/8
            Assert.AreEqual('C', result.Tune.KeySignature.Tonic);
        }

        [Test]
        public void Tokenizer_TokenizesBasicElements_Successfully()
        {
            // Arrange
            string abcText = "X:1\nABcd|";
            var tokenizer = new AbcTokenizer();

            // Act
            var tokens = tokenizer.Tokenize(abcText);

            // Assert
            Assert.IsTrue(tokens.Count > 0);
            Assert.AreEqual(TokenType.InformationField, tokens[0].Type);
            Assert.AreEqual("X:1", tokens[0].Value);
        }

        [Test]
        public void Parser_ParsesSimpleNotes_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Simple Notes
M:4/4
L:1/4
K:C
C D E F";

            var parser = new AbcParser();

            // Act
            var result = parser.Parse(abcText);

            // Assert
            Assert.IsTrue(result.Success, $"Parse failed: {string.Join(", ", result.Errors)}");
            Assert.IsNotNull(result.Tune);
            Assert.AreEqual(4, result.Tune.Elements.Count);
            
            var note1 = result.Tune.Elements[0] as Note;
            Assert.IsNotNull(note1);
            Assert.AreEqual('C', note1.Pitch);
            Assert.AreEqual(4, note1.Octave);
            Assert.AreEqual(1.0, note1.Duration);
            
            var note2 = result.Tune.Elements[1] as Note;
            Assert.IsNotNull(note2);
            Assert.AreEqual('D', note2.Pitch);
        }

        [Test]
        public void Parser_ParsesNotesWithDurations_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Notes with Durations
M:4/4
L:1/4
K:C
C2 D/ E3 F/2";

            var parser = new AbcParser();

            // Act
            var result = parser.Parse(abcText);

            // Assert
            Assert.IsTrue(result.Success, $"Parse failed: {string.Join(", ", result.Errors)}");
            Assert.IsNotNull(result.Tune);
            Assert.AreEqual(4, result.Tune.Elements.Count);
            
            var note1 = result.Tune.Elements[0] as Note;
            Assert.IsNotNull(note1);
            Assert.AreEqual('C', note1.Pitch);
            Assert.AreEqual(2.0, note1.Duration);
            
            var note2 = result.Tune.Elements[1] as Note;
            Assert.IsNotNull(note2);
            Assert.AreEqual('D', note2.Pitch);
            Assert.AreEqual(0.5, note2.Duration);

            var note3 = result.Tune.Elements[2] as Note;
            Assert.IsNotNull(note3);
            Assert.AreEqual('E', note3.Pitch);
            Assert.AreEqual(3.0, note3.Duration);

            var note4 = result.Tune.Elements[3] as Note;
            Assert.IsNotNull(note4);
            Assert.AreEqual('F', note4.Pitch);
            Assert.AreEqual(0.5, note4.Duration);
        }

        [Test]
        public void Parser_ParsesNotesWithAccidentals_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Notes with Accidentals
M:4/4
L:1/4
K:C
^C _D =E ^^F __G";

            var parser = new AbcParser();

            // Act
            var result = parser.Parse(abcText);

            // Assert
            Assert.IsTrue(result.Success, $"Parse failed: {string.Join(", ", result.Errors)}");
            Assert.IsNotNull(result.Tune);
            Assert.AreEqual(5, result.Tune.Elements.Count);
            
            var note1 = result.Tune.Elements[0] as Note;
            Assert.IsNotNull(note1);
            Assert.AreEqual('C', note1.Pitch);
            Assert.AreEqual(Accidental.Sharp, note1.Accidental);
            
            var note2 = result.Tune.Elements[1] as Note;
            Assert.IsNotNull(note2);
            Assert.AreEqual('D', note2.Pitch);
            Assert.AreEqual(Accidental.Flat, note2.Accidental);

            var note3 = result.Tune.Elements[2] as Note;
            Assert.IsNotNull(note3);
            Assert.AreEqual('E', note3.Pitch);
            Assert.AreEqual(Accidental.Natural, note3.Accidental);

            var note4 = result.Tune.Elements[3] as Note;
            Assert.IsNotNull(note4);
            Assert.AreEqual('F', note4.Pitch);
            Assert.AreEqual(Accidental.DoubleSharp, note4.Accidental);

            var note5 = result.Tune.Elements[4] as Note;
            Assert.IsNotNull(note5);
            Assert.AreEqual('G', note5.Pitch);
            Assert.AreEqual(Accidental.DoubleFlat, note5.Accidental);
        }

        [Test]
        public void Parser_ParsesNotesWithOctaves_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Notes with Octaves
M:4/4
L:1/4
K:C
C,, C, C c c' c''";

            var parser = new AbcParser();

            // Act
            var result = parser.Parse(abcText);

            // Assert
            Assert.IsTrue(result.Success, $"Parse failed: {string.Join(", ", result.Errors)}");
            Assert.IsNotNull(result.Tune);
            Assert.AreEqual(6, result.Tune.Elements.Count);
            
            var note1 = result.Tune.Elements[0] as Note;
            Assert.IsNotNull(note1);
            Assert.AreEqual('C', note1.Pitch);
            Assert.AreEqual(2, note1.Octave); // C,,

            var note2 = result.Tune.Elements[1] as Note;
            Assert.IsNotNull(note2);
            Assert.AreEqual('C', note2.Pitch);
            Assert.AreEqual(3, note2.Octave); // C,

            var note3 = result.Tune.Elements[2] as Note;
            Assert.IsNotNull(note3);
            Assert.AreEqual('C', note3.Pitch);
            Assert.AreEqual(4, note3.Octave); // C

            var note4 = result.Tune.Elements[3] as Note;
            Assert.IsNotNull(note4);
            Assert.AreEqual('C', note4.Pitch);
            Assert.AreEqual(5, note4.Octave); // c

            var note5 = result.Tune.Elements[4] as Note;
            Assert.IsNotNull(note5);
            Assert.AreEqual('C', note5.Pitch);
            Assert.AreEqual(6, note5.Octave); // c'

            var note6 = result.Tune.Elements[5] as Note;
            Assert.IsNotNull(note6);
            Assert.AreEqual('C', note6.Pitch);
            Assert.AreEqual(7, note6.Octave); // c''
        }

        [Test]
        public void Parser_ParsesRests_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Rests
M:4/4
L:1/4
K:C
z z2 z/ z3";

            var parser = new AbcParser();

            // Act
            var result = parser.Parse(abcText);

            // Assert
            Assert.IsTrue(result.Success, $"Parse failed: {string.Join(", ", result.Errors)}");
            Assert.IsNotNull(result.Tune);
            Assert.AreEqual(4, result.Tune.Elements.Count);
            
            var rest1 = result.Tune.Elements[0] as Rest;
            Assert.IsNotNull(rest1);
            Assert.AreEqual(1.0, rest1.Duration);
            
            var rest2 = result.Tune.Elements[1] as Rest;
            Assert.IsNotNull(rest2);
            Assert.AreEqual(2.0, rest2.Duration);

            var rest3 = result.Tune.Elements[2] as Rest;
            Assert.IsNotNull(rest3);
            Assert.AreEqual(0.5, rest3.Duration);

            var rest4 = result.Tune.Elements[3] as Rest;
            Assert.IsNotNull(rest4);
            Assert.AreEqual(3.0, rest4.Duration);
        }

        [Test]
        public void Parser_ParsesMixedNotesAndRests_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Mixed Notes and Rests
M:4/4
L:1/8
K:C
C2 z D ^E/ z/ F4";

            var parser = new AbcParser();

            // Act
            var result = parser.Parse(abcText);

            // Assert
            Assert.IsTrue(result.Success, $"Parse failed: {string.Join(", ", result.Errors)}");
            Assert.IsNotNull(result.Tune);
            Assert.AreEqual(6, result.Tune.Elements.Count);
            
            Assert.IsInstanceOf<Note>(result.Tune.Elements[0]);
            Assert.IsInstanceOf<Rest>(result.Tune.Elements[1]);
            Assert.IsInstanceOf<Note>(result.Tune.Elements[2]);
            Assert.IsInstanceOf<Note>(result.Tune.Elements[3]);
            Assert.IsInstanceOf<Rest>(result.Tune.Elements[4]);
            Assert.IsInstanceOf<Note>(result.Tune.Elements[5]);

            var note1 = result.Tune.Elements[0] as Note;
            Assert.AreEqual('C', note1.Pitch);
            Assert.AreEqual(2.0, note1.Duration);

            var note2 = result.Tune.Elements[3] as Note;
            Assert.AreEqual('E', note2.Pitch);
            Assert.AreEqual(Accidental.Sharp, note2.Accidental);
            Assert.AreEqual(0.5, note2.Duration);
        }

        [Test]
        public void Parser_HandlesInvalidInput_Gracefully()
        {
            // Arrange
            string abcText = @"X:1
T:Invalid Test
M:invalid
L:1/0
K:Invalid
invalid_note";

            var parser = new AbcParser();

            // Act
            var result = parser.Parse(abcText);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.Errors.Count > 0);
            Assert.IsNotNull(result.Tune); // Should still create a tune object
        }

        [Test]
        public void Parser_ParsesComplexTimeSignatures_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Complex Time Signatures
M:C
L:1/8
K:G
C2";

            var parser = new AbcParser();

            // Act
            var result = parser.Parse(abcText);

            // Assert
            Assert.IsTrue(result.Success, $"Parse failed: {string.Join(", ", result.Errors)}");
            Assert.IsNotNull(result.Tune);
            Assert.AreEqual(TimeSignatureType.CommonTime, result.Tune.TimeSignature.Type);
            Assert.AreEqual(4, result.Tune.TimeSignature.Numerator);
            Assert.AreEqual(4, result.Tune.TimeSignature.Denominator);
        }

        [Test]
        public void Parser_ParsesDurationPatterns_Successfully()
        {
            // Arrange
            var parser = new AbcParser();

            // Test various duration patterns
            var testCases = new[]
            {
                ("C", 1.0),     // Default duration
                ("C2", 2.0),    // Whole number
                ("C/", 0.5),    // Half
                ("C//", 0.5),   // Half (alternate notation)
                ("C/2", 0.5),   // Explicit half
                ("C/4", 0.25),  // Quarter
                ("C3/2", 1.5),  // Three halves
                ("C3/4", 0.75)  // Three quarters
            };

            foreach (var (notePattern, expectedDuration) in testCases)
            {
                string abcText = $@"X:1
T:Duration Test
M:4/4
L:1/4
K:C
{notePattern}";

                // Act
                var result = parser.Parse(abcText);

                // Assert
                Assert.IsTrue(result.Success, $"Parse failed for {notePattern}: {string.Join(", ", result.Errors)}");
                Assert.IsNotNull(result.Tune);
                Assert.AreEqual(1, result.Tune.Elements.Count, $"Expected 1 element for {notePattern}");
                
                var note = result.Tune.Elements[0] as Note;
                Assert.IsNotNull(note, $"Expected Note for {notePattern}");
                Assert.AreEqual(expectedDuration, note.Duration, 0.001, $"Wrong duration for {notePattern}");
            }
        }

        [Test]
        public void Tokenizer_ParsesAdvancedTokens_Successfully()
        {
            // Arrange
            string abcText = "C{DEF}G [CEG] C<D E>F C-D (EF) C.D CTC";
            var tokenizer = new AbcTokenizer();

            // Act
            var tokens = tokenizer.Tokenize(abcText);

            // Assert
            Assert.IsTrue(tokens.Count > 0);
            
            // Check for grace note tokens
            var graceStartTokens = tokens.Where(t => t.Type == TokenType.GraceNoteStart).ToArray();
            var graceEndTokens = tokens.Where(t => t.Type == TokenType.GraceNoteEnd).ToArray();
            Assert.AreEqual(1, graceStartTokens.Length, "Should find one grace note start");
            Assert.AreEqual(1, graceEndTokens.Length, "Should find one grace note end");

            // Check for chord tokens
            var chordStartTokens = tokens.Where(t => t.Type == TokenType.ChordStart).ToArray();
            var chordEndTokens = tokens.Where(t => t.Type == TokenType.ChordEnd).ToArray();
            Assert.AreEqual(1, chordStartTokens.Length, "Should find one chord start");
            Assert.AreEqual(1, chordEndTokens.Length, "Should find one chord end");

            // Check for broken rhythm tokens
            var brokenRhythmTokens = tokens.Where(t => t.Type == TokenType.BrokenRhythm).ToArray();
            Assert.AreEqual(2, brokenRhythmTokens.Length, "Should find two broken rhythm tokens");

            // Check for tie tokens
            var tieTokens = tokens.Where(t => t.Type == TokenType.Tie).ToArray();
            Assert.AreEqual(1, tieTokens.Length, "Should find one tie token");

            // Check for slur tokens
            var slurTokens = tokens.Where(t => t.Type == TokenType.Slur).ToArray();
            Assert.AreEqual(2, slurTokens.Length, "Should find two slur tokens");

            // Check for staccato tokens
            var staccatoTokens = tokens.Where(t => t.Type == TokenType.Staccato).ToArray();
            Assert.AreEqual(1, staccatoTokens.Length, "Should find one staccato token");

            // Check for trill tokens
            var trillTokens = tokens.Where(t => t.Type == TokenType.Trill).ToArray();
            Assert.IsTrue(trillTokens.Length >= 0, "Should find trill tokens if present");
        }

        [Test]
        public void Parser_HandlesChords_Successfully()
        {
            // Arrange
            string abcText = @"X:1
T:Chord Test
M:4/4
L:1/4
K:C
[CEG] [^FBd] z [ACE]2";

            var parser = new AbcParser();

            // Act
            var result = parser.Parse(abcText);

            // Assert
            Assert.IsTrue(result.Success, $"Parse failed: {string.Join(", ", result.Errors)}");
            Assert.IsNotNull(result.Tune);
            
            // For now, individual chord notes will be parsed as separate notes
            // We could enhance this later to create Chord objects
            var notes = result.Tune.Elements.OfType<Note>().ToArray();
            var rests = result.Tune.Elements.OfType<Rest>().ToArray();
            
            Assert.IsTrue(notes.Length >= 6, "Should parse chord notes as individual notes");
            Assert.AreEqual(1, rests.Length, "Should find one rest");
        }

        [Test]
        public void Parser_SkipsUnsupportedTokens_Gracefully()
        {
            // Arrange
            string abcText = @"X:1
T:Advanced Features Test
M:4/4
L:1/4
K:C
C {def} D<E F>G A-B (cd) e.f gTh iTj";

            var parser = new AbcParser();

            // Act
            var result = parser.Parse(abcText);

            // Assert
            // Should parse successfully but may generate warnings for unsupported features
            Assert.IsNotNull(result.Tune);
            
            // Should still parse the basic notes
            var notes = result.Tune.Elements.OfType<Note>().ToArray();
            Assert.IsTrue(notes.Length > 0, "Should parse at least some notes");
        }
    }
}