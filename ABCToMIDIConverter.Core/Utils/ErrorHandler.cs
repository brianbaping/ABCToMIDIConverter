using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ABCToMIDIConverter.Core.Utils
{
    /// <summary>
    /// Enhanced error handling and validation utilities
    /// </summary>
    public static class ErrorHandler
    {
        /// <summary>
        /// Validates ABC notation content
        /// </summary>
        public static ValidationResult ValidateAbcContent(string abcContent)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(abcContent))
            {
                result.AddError("ABC content cannot be empty");
                return result;
            }

            var lines = abcContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            bool hasReferenceNumber = false;
            bool hasTitle = false;
            bool hasKey = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('%'))
                    continue;

                if (trimmed.StartsWith("X:"))
                    hasReferenceNumber = true;
                else if (trimmed.StartsWith("T:"))
                    hasTitle = true;
                else if (trimmed.StartsWith("K:"))
                    hasKey = true;
            }

            if (!hasReferenceNumber)
                result.AddWarning("Missing reference number (X:). This is recommended for ABC notation.");
            
            if (!hasTitle)
                result.AddWarning("Missing title (T:). Consider adding a title for better organization.");
            
            if (!hasKey)
                result.AddError("Missing key signature (K:). This is required for ABC notation.");

            return result;
        }

        /// <summary>
        /// Validates file path for MIDI output
        /// </summary>
        public static ValidationResult ValidateOutputPath(string outputPath)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                result.AddError("Output path cannot be empty");
                return result;
            }

            try
            {
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    result.AddWarning($"Directory does not exist: {directory}. It will be created.");
                }

                var fileName = Path.GetFileName(outputPath);
                if (string.IsNullOrEmpty(fileName))
                {
                    result.AddError("Invalid file name");
                    return result;
                }

                var extension = Path.GetExtension(outputPath).ToLowerInvariant();
                if (extension != ".mid" && extension != ".midi")
                {
                    result.AddWarning($"File extension '{extension}' is not standard for MIDI files. Consider using .mid or .midi");
                }

                // Check for invalid characters
                var invalidChars = Path.GetInvalidPathChars();
                foreach (char c in outputPath)
                {
                    if (Array.IndexOf(invalidChars, c) >= 0)
                    {
                        result.AddError($"Invalid character in path: '{c}'");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Invalid path format: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Logs errors with structured information
        /// </summary>
        public static void LogError(ILogger? logger, string operation, Exception exception, object? context = null)
        {
            if (logger == null) return;

            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["Operation"] = operation,
                ["Context"] = context?.ToString() ?? "None"
            });

            logger.LogError(exception, "Error during {Operation}", operation);
        }

        /// <summary>
        /// Logs warnings with context
        /// </summary>
        public static void LogWarning(ILogger? logger, string operation, string message, object? context = null)
        {
            if (logger == null) return;

            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["Operation"] = operation,
                ["Context"] = context?.ToString() ?? "None"
            });

            logger.LogWarning("Warning during {Operation}: {Message}", operation, message);
        }
    }

    /// <summary>
    /// Represents the result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();

        public bool IsValid => Errors.Count == 0;
        public bool HasWarnings => Warnings.Count > 0;

        public void AddError(string error)
        {
            Errors.Add(error);
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        public void AddErrors(IEnumerable<string> errors)
        {
            Errors.AddRange(errors);
        }

        public void AddWarnings(IEnumerable<string> warnings)
        {
            Warnings.AddRange(warnings);
        }

        public override string ToString()
        {
            var result = new List<string>();
            
            if (Errors.Count > 0)
            {
                result.Add($"Errors ({Errors.Count}):");
                result.AddRange(Errors.Select(e => $"  • {e}"));
            }

            if (Warnings.Count > 0)
            {
                result.Add($"Warnings ({Warnings.Count}):");
                result.AddRange(Warnings.Select(w => $"  • {w}"));
            }

            return string.Join("\n", result);
        }
    }
}
