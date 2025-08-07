using Microsoft.Extensions.Logging;
using ABCToMIDIConverter.Core.Models;

namespace ABCToMIDIConverter.Core.Converters
{
    /// <summary>
    /// Enhanced MIDI converter with improved error handling and logging
    /// </summary>
    public class EnhancedMidiConverter
    {
        private readonly ILogger<EnhancedMidiConverter>? _logger;
        private readonly MidiConverter _midiConverter;
        
        public EnhancedMidiConverter(ILogger<EnhancedMidiConverter>? logger = null)
        {
            _logger = logger;
            _midiConverter = new MidiConverter();
        }

        /// <summary>
        /// Converts an ABC tune to MIDI with comprehensive error handling
        /// </summary>
        public async Task<ConversionResult> ConvertToMidiFileAsync(AbcTune tune, string outputPath, CancellationToken cancellationToken = default)
        {
            var result = new ConversionResult();
            
            try
            {
                // Validate inputs
                if (tune == null)
                {
                    result.AddError("Tune cannot be null");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    result.AddError("Output path cannot be empty");
                    return result;
                }

                // Validate tune content
                var validationResult = ValidateTune(tune);
                if (!validationResult.IsValid)
                {
                    result.Errors.AddRange(validationResult.Errors);
                    result.Warnings.AddRange(validationResult.Warnings);
                    
                    if (validationResult.HasCriticalErrors)
                    {
                        return result;
                    }
                }

                _logger?.LogInformation("Starting MIDI conversion for tune: {Title}", tune.Title);

                // Create output directory if it doesn't exist
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger?.LogDebug("Created output directory: {Directory}", directory);
                }

                // Perform conversion
                await Task.Run(() => _midiConverter.ConvertToMidiFile(tune, outputPath), cancellationToken);

                // Verify output file was created
                if (File.Exists(outputPath))
                {
                    var fileInfo = new FileInfo(outputPath);
                    result.IsSuccess = true;
                    result.OutputPath = outputPath;
                    result.FileSizeBytes = fileInfo.Length;
                    
                    _logger?.LogInformation("MIDI conversion completed successfully. Output: {OutputPath}, Size: {Size} bytes", 
                        outputPath, fileInfo.Length);
                }
                else
                {
                    result.AddError("MIDI file was not created");
                }
            }
            catch (OperationCanceledException)
            {
                result.AddError("Conversion was cancelled");
                _logger?.LogWarning("MIDI conversion was cancelled");
            }
            catch (UnauthorizedAccessException ex)
            {
                result.AddError($"Access denied: {ex.Message}");
                _logger?.LogError(ex, "Access denied during MIDI conversion");
            }
            catch (DirectoryNotFoundException ex)
            {
                result.AddError($"Directory not found: {ex.Message}");
                _logger?.LogError(ex, "Directory not found during MIDI conversion");
            }
            catch (IOException ex)
            {
                result.AddError($"IO error: {ex.Message}");
                _logger?.LogError(ex, "IO error during MIDI conversion");
            }
            catch (Exception ex)
            {
                result.AddError($"Unexpected error: {ex.Message}");
                _logger?.LogError(ex, "Unexpected error during MIDI conversion");
            }

            return result;
        }

        private ValidationResult ValidateTune(AbcTune tune)
        {
            var result = new ValidationResult();

            // Check for required fields
            if (string.IsNullOrWhiteSpace(tune.Title))
            {
                result.AddWarning("Tune has no title");
            }

            if (tune.Elements == null || tune.Elements.Count == 0)
            {
                result.AddCriticalError("Tune has no musical elements");
                return result;
            }

            // Validate time signature
            if (tune.TimeSignature.Numerator <= 0 || tune.TimeSignature.Denominator <= 0)
            {
                result.AddCriticalError("Invalid time signature");
            }

            // Check for valid tempo
            if (tune.Tempo <= 0 || tune.Tempo > 300)
            {
                result.AddWarning($"Unusual tempo: {tune.Tempo} BPM");
            }

            // Validate musical elements
            foreach (var element in tune.Elements)
            {
                if (element is Note note)
                {
                    if (note.Duration <= 0)
                    {
                        result.AddError($"Note has invalid duration: {note.Duration}");
                    }

                    if (note.Octave < 0 || note.Octave > 9)
                    {
                        result.AddWarning($"Note has unusual octave: {note.Octave}");
                    }
                }
                else if (element is Rest rest)
                {
                    if (rest.Duration <= 0)
                    {
                        result.AddError($"Rest has invalid duration: {rest.Duration}");
                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Result of a MIDI conversion operation
    /// </summary>
    public class ConversionResult
    {
        public bool IsSuccess { get; set; }
        public string? OutputPath { get; set; }
        public long FileSizeBytes { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public TimeSpan Duration { get; set; }

        public void AddError(string message) => Errors.Add(message);
        public void AddWarning(string message) => Warnings.Add(message);
        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;
    }

    /// <summary>
    /// Result of tune validation
    /// </summary>
    internal class ValidationResult
    {
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool IsValid => !HasCriticalErrors;
        public bool HasCriticalErrors { get; private set; }

        public void AddError(string message) => Errors.Add(message);
        public void AddWarning(string message) => Warnings.Add(message);
        public void AddCriticalError(string message)
        {
            Errors.Add(message);
            HasCriticalErrors = true;
        }
    }
}
