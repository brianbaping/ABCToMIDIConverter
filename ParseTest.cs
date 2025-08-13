using System;
using System.IO;
using ABCToMIDIConverter.Core.Parsers;

namespace ParseTest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var abcContent = File.ReadAllText(@"d:\Brian\Development\ABCToMIDIConverter\test_sample.abc");
                var parser = new AbcParser();
                var result = parser.Parse(abcContent);

                Console.WriteLine($"Parse Success: {result.Success}");
                Console.WriteLine($"Errors: {result.Errors.Count}");
                Console.WriteLine($"Warnings: {result.Warnings.Count}");
                
                if (result.Tune != null)
                {
                    Console.WriteLine($"Title: {result.Tune.Title}");
                    Console.WriteLine($"Elements: {result.Tune.Elements.Count}");
                    
                    foreach (var element in result.Tune.Elements)
                    {
                        Console.WriteLine($"  {element.GetType().Name}: {element}");
                    }
                }

                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"ERROR: {error}");
                }

                foreach (var warning in result.Warnings)
                {
                    Console.WriteLine($"WARNING: {warning}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
    }
}
