using System.Collections.Generic;
using ABCToMIDIConverter.Core.Models;

namespace ABCToMIDIConverter.Core.Parsers
{
    /// <summary>
    /// Result of parsing an ABC file
    /// </summary>
    public class ParseResult
    {
        public AbcTune? Tune { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public bool Success => Tune != null && Errors.Count == 0;

        public void AddError(string message, int line = 0, int column = 0)
        {
            var location = line > 0 ? $" at line {line}" : "";
            if (column > 0) location += $", column {column}";
            Errors.Add($"{message}{location}");
        }

        public void AddWarning(string message, int line = 0, int column = 0)
        {
            var location = line > 0 ? $" at line {line}" : "";
            if (column > 0) location += $", column {column}";
            Warnings.Add($"{message}{location}");
        }
    }
}