using NUnit.Framework;
using ABCToMIDIConverter.Core.Parsers;

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
    }
}