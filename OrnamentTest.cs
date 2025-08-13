using System;
using ABCToMIDIConverter.Core.Parsers;

namespace ParseTest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var abcContent = @"X:1
T:Ornament Test
M:4/4
L:1/4
K:C
CT";
                var tokenizer = new AbcTokenizer();
                var tokens = tokenizer.Tokenize(abcContent);
                
                Console.WriteLine("Tokens found:");
                foreach (var token in tokens)
                {
                    Console.WriteLine($"  {token.Type}: '{token.Value}' at {token.Line}:{token.Column}");
                }
                
                Console.WriteLine("\nParsing:");
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
