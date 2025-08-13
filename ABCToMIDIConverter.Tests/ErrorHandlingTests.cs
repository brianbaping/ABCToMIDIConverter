using NUnit.Framework;
using ABCToMIDIConverter.Core.Parsers;
using System;

namespace ABCToMIDIConverter.Tests
{
    /// <summary>
    /// Tests specifically for error message handling and display
    /// </summary>
    [TestFixture]
    public class ErrorHandlingTests
    {
        private AbcParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new AbcParser();
        }

        [Test]
        public void ParseResult_HasCorrectSuccessFlag_WhenErrors()
        {
            // Test that when errors are added, Success is false
            var result = _parser.Parse("X:1\nT:Test\nM:4/4\nK:C\nInvalidToken$%^&*");
            
            Assert.That(result.Success, Is.False, "Result should be marked as unsuccessful when errors occur");
            Assert.That(result.Errors.Count, Is.GreaterThan(0), "Should have at least one error message");
            Assert.That(result.Tune, Is.Null, "Tune should be null when parsing fails");
        }

        [Test]
        public void ParseResult_WithTimeout_HasCorrectErrorMessage()
        {
            // Create a string that should trigger timeout (very short timeout)
            var result = _parser.Parse("X:1\nT:Test\nM:4/4\nK:C\nC C C C", timeoutSeconds: 0); // 0 seconds timeout
            
            Assert.That(result.Success, Is.False, "Result should be marked as unsuccessful when timeout occurs");
            Assert.That(result.Errors.Count, Is.GreaterThan(0), "Should have timeout error message");
            Assert.That(result.Tune, Is.Null, "Tune should be null when timeout occurs");
            
            // Check that the error message mentions timeout/cancellation
            var errorMessage = string.Join(" ", result.Errors);
            Assert.That(errorMessage.ToLower(), Does.Contain("cancel").Or.Contain("timeout"), 
                "Error message should mention cancellation or timeout");
        }

        [Test]
        public void ParseResult_WithValidInput_HasSuccess()
        {
            // Test that valid input still works correctly
            var result = _parser.Parse("X:1\nT:Test\nM:4/4\nK:C\nC D E F");
            
            Assert.That(result.Success, Is.True, "Result should be successful for valid input");
            Assert.That(result.Errors.Count, Is.EqualTo(0), "Should have no errors for valid input");
            Assert.That(result.Tune, Is.Not.Null, "Tune should be created for valid input");
        }

        [Test]
        public void ParseResult_WithInvalidHeader_HasErrorMessage()
        {
            // Test that invalid header generates appropriate error
            var result = _parser.Parse("InvalidHeader\nT:Test\nM:4/4\nK:C");
            
            Assert.That(result.Success, Is.False, "Result should be unsuccessful for invalid header");
            Assert.That(result.Errors.Count, Is.GreaterThan(0), "Should have error messages");
            Assert.That(result.Tune, Is.Null, "Tune should be null when parsing fails");
        }
    }
}
