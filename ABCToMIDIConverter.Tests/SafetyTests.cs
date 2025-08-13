using NUnit.Framework;
using ABCToMIDIConverter.Core.Parsers;
using ABCToMIDIConverter.Core.Converters;
using System;
using System.Text;

namespace ABCToMIDIConverter.Tests
{
    /// <summary>
    /// Tests for safety measures to prevent infinite loops and lockups
    /// </summary>
    [TestFixture]
    public class SafetyTests
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
        public void Parser_HandlesVeryLargeInput_WithoutLocking()
        {
            // Create a very large but valid ABC notation
            var sb = new StringBuilder();
            sb.AppendLine("X:1");
            sb.AppendLine("T:Large Test");
            sb.AppendLine("M:4/4");
            sb.AppendLine("L:1/4");
            sb.AppendLine("K:C");
            
            // Add many notes (but within reasonable limits)
            for (int i = 0; i < 1000; i++)
            {
                sb.Append("C D E F G A B c ");
            }
            
            var result = _parser.Parse(sb.ToString());
            
            // Should succeed without timing out
            Assert.IsTrue(result.Success, $"Parse failed: {string.Join(", ", result.Errors)}");
            Assert.IsNotNull(result.Tune);
        }

        [Test]
        public void Parser_HandlesReasonablyLargeInput_WithoutIssues()
        {
            // Test with a large but reasonable input to ensure it doesn't cause memory issues
            // This is more of a performance/stability test than a limit test
            var sb = new StringBuilder();
            sb.AppendLine("X:1");
            sb.AppendLine("T:Large Performance Test");
            sb.AppendLine("M:4/4");
            sb.AppendLine("L:1/4");
            sb.AppendLine("K:C");
            
            // Add a reasonable number of musical elements
            for (int i = 0; i < 5000; i++)
            {
                sb.Append("C D E F ");
            }
            
            var input = sb.ToString();
            Console.WriteLine($"Input size: {input.Length:N0} characters");

            var result = _parser.Parse(input);
            
            Assert.IsTrue(result.Success, $"Parse should succeed: {string.Join(", ", result.Errors)}");
            Assert.IsNotNull(result.Tune);
            Assert.Greater(result.Tune.Elements.Count, 15000, "Should have parsed many notes");
        }

        [Test]
        public void Parser_HandlesInvalidTokens_Gracefully()
        {
            // ABC notation with unknown/invalid characters that might cause tokenizer issues
            string invalidAbc = @"
X:1
T:Invalid Test
M:4/4
L:1/4
K:C
C D @#$%^&*() E F G";

            var result = _parser.Parse(invalidAbc);
            
            // Should parse what it can and report warnings, not crash
            Assert.IsTrue(result.Success || result.Warnings.Count > 0);
            Assert.IsNotNull(result.Tune);
        }

        [Test]
        public void MidiConverter_HandlesComplexTune_WithTimeout()
        {
            string complexAbc = @"
X:1
T:Complex Test
M:4/4
L:1/16
K:C
";
            // Add many ornaments and dynamics
            var sb = new StringBuilder(complexAbc);
            for (int i = 0; i < 100; i++)
            {
                sb.Append("pp CT MD ~S {CDEFG} ff A B c d e f g ");
            }

            var result = _parser.Parse(sb.ToString());
            Assert.IsTrue(result.Success, $"Parse failed: {string.Join(", ", result.Errors)}");
            Assert.IsNotNull(result.Tune);

            // MIDI conversion should complete within timeout
            var tempFile = System.IO.Path.GetTempFileName() + ".mid";
            try
            {
                // Use a reasonable timeout
                _converter.ConvertToMidiFile(result.Tune!, tempFile, timeoutSeconds: 10);
                
                Assert.IsTrue(System.IO.File.Exists(tempFile), "MIDI file should be created");
            }
            finally
            {
                if (System.IO.File.Exists(tempFile))
                    System.IO.File.Delete(tempFile);
            }
        }

        [Test]
        public void Parser_HandlesInfiniteLoopPattern_Safely()
        {
            // Pattern that might cause parsing issues
            string problematicAbc = @"
X:1
T:Problematic Test
M:4/4
L:1/4
K:C
C%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%D E F";

            var result = _parser.Parse(problematicAbc);
            
            // Should handle gracefully without infinite loop
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Tune);
        }
    }
}
