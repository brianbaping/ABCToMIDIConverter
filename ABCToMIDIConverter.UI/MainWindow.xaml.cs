using System;
using System.IO;
using System.Windows;
using ABCToMIDIConverter.Core.Parsers;
using ABCToMIDIConverter.Core.Converters;
using Microsoft.Win32;

namespace ABCToMIDIConverter.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ParseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusTextBlock.Text = "Parsing...";
                OutputTextBox.Clear();

                var parser = new AbcParser();
                var result = parser.Parse(AbcTextBox.Text);

                if (result.Success && result.Tune != null)
                {
                    OutputTextBox.AppendText("✓ Parsing successful!\n\n");
                    OutputTextBox.AppendText($"Reference: {result.Tune.ReferenceNumber}\n");
                    OutputTextBox.AppendText($"Title: {result.Tune.Title}\n");
                    OutputTextBox.AppendText($"Composer: {result.Tune.Composer}\n");
                    OutputTextBox.AppendText($"Time Signature: {result.Tune.TimeSignature}\n");
                    OutputTextBox.AppendText($"Unit Note Length: {result.Tune.UnitNoteLength}\n");
                    OutputTextBox.AppendText($"Key: {result.Tune.KeySignature}\n");
                    OutputTextBox.AppendText($"Tempo: {result.Tune.Tempo} BPM\n");

                    if (result.Warnings.Count > 0)
                    {
                        OutputTextBox.AppendText("\nWarnings:\n");
                        foreach (var warning in result.Warnings)
                        {
                            OutputTextBox.AppendText($"⚠ {warning}\n");
                        }
                    }

                    StatusTextBlock.Text = "Parsing completed successfully";
                }
                else
                {
                    OutputTextBox.AppendText("✗ Parsing failed!\n\n");
                    foreach (var error in result.Errors)
                    {
                        OutputTextBox.AppendText($"❌ {error}\n");
                    }
                    StatusTextBlock.Text = "Parsing failed";
                }
            }
            catch (Exception ex)
            {
                OutputTextBox.Text = $"Error: {ex.Message}";
                StatusTextBlock.Text = "Error occurred";
                MessageBox.Show($"An error occurred during parsing:\n{ex.Message}",
                              "Parsing Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusTextBlock.Text = "Converting to MIDI...";

                // First parse the ABC
                var parser = new AbcParser();
                var parseResult = parser.Parse(AbcTextBox.Text);

                if (!parseResult.Success || parseResult.Tune == null)
                {
                    MessageBox.Show("Please parse the ABC notation first and fix any errors.",
                                  "Parsing Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StatusTextBlock.Text = "Ready";
                    return;
                }

                // Choose save location
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "MIDI files (*.mid)|*.mid|All files (*.*)|*.*",
                    DefaultExt = "mid",
                    FileName = $"{SanitizeFileName(parseResult.Tune.Title)}.mid"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        // Convert to MIDI using the simple converter
                        var converter = new MidiConverter();
                        converter.ConvertToMidiFile(parseResult.Tune, saveFileDialog.FileName);

                        OutputTextBox.AppendText($"\n✓ MIDI file created: {saveFileDialog.FileName}\n");
                        StatusTextBlock.Text = "MIDI conversion completed";

                        MessageBox.Show($"MIDI file created successfully!\n\nSaved to: {saveFileDialog.FileName}",
                                      "Conversion Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        MessageBox.Show($"Cannot write to the selected location. Please choose a different location or run as administrator.",
                                      "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusTextBlock.Text = "Conversion failed - Access denied";
                    }
                    catch (DirectoryNotFoundException)
                    {
                        MessageBox.Show($"The directory does not exist. Please choose a valid location.",
                                      "Directory Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusTextBlock.Text = "Conversion failed - Directory not found";
                    }
                }
                else
                {
                    StatusTextBlock.Text = "Ready";
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Conversion failed: {ex.Message}";
                OutputTextBox.AppendText($"\n❌ {errorMsg}\n");
                StatusTextBlock.Text = "Conversion failed";
                MessageBox.Show($"An error occurred during conversion:\n{ex.Message}\n\nPlease check that the ABC notation is valid and try again.",
                              "Conversion Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Clear();
            StatusTextBlock.Text = "Ready";
        }

        /// <summary>
        /// Sanitizes a filename by removing or replacing invalid characters
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "untitled";

            // Replace invalid characters with underscores
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            // Remove extra spaces and limit length
            fileName = fileName.Trim().Replace("  ", " ");
            if (fileName.Length > 50)
                fileName = fileName.Substring(0, 50);

            return string.IsNullOrWhiteSpace(fileName) ? "untitled" : fileName;
        }
    }
}