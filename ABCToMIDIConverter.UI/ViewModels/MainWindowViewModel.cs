using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using ABCToMIDIConverter.Core.Parsers;
using ABCToMIDIConverter.Core.Converters;
using ABCToMIDIConverter.Core.Models;
using Microsoft.Extensions.Logging;

namespace ABCToMIDIConverter.UI.ViewModels
{
    /// <summary>
    /// Main window view model with proper MVVM pattern
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly AbcParser _parser;
        private readonly EnhancedMidiConverter _converter;
        private readonly ILogger<MainWindowViewModel>? _logger;

        private string _abcText = "";
        private string _outputMessages = "";
        private string _statusText = "Ready";
        private bool _isConverting = false;
        private AbcTune? _currentTune;

        public MainWindowViewModel(ILogger<MainWindowViewModel>? logger = null)
        {
            _logger = logger;
            _parser = new AbcParser();
            _converter = new EnhancedMidiConverter();

            // Initialize commands
            ParseCommand = new RelayCommand(ExecuteParse, CanExecuteParse);
            ConvertCommand = new RelayCommand(ExecuteConvert, CanExecuteConvert);
            ClearCommand = new RelayCommand(ExecuteClear);
            LoadSampleCommand = new RelayCommand(ExecuteLoadSample);

            // Initialize with sample ABC
            LoadSample();
        }

        #region Properties

        public string AbcText
        {
            get => _abcText;
            set
            {
                if (SetProperty(ref _abcText, value))
                {
                    _currentTune = null; // Clear parsed tune when text changes
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string OutputMessages
        {
            get => _outputMessages;
            set => SetProperty(ref _outputMessages, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public bool IsConverting
        {
            get => _isConverting;
            set
            {
                if (SetProperty(ref _isConverting, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ObservableCollection<string> RecentFiles { get; } = new();

        #endregion

        #region Commands

        public ICommand ParseCommand { get; }
        public ICommand ConvertCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand LoadSampleCommand { get; }

        private bool CanExecuteParse()
        {
            return !string.IsNullOrWhiteSpace(AbcText) && !IsConverting;
        }

        private void ExecuteParse()
        {
            try
            {
                StatusText = "Parsing...";
                OutputMessages = "🎵 Parsing ABC notation...\n";

                var result = _parser.Parse(AbcText);

                if (result.Success)
                {
                    _currentTune = result.Tune;
                    OutputMessages += $"✅ Parsing successful!\n";
                    OutputMessages += $"📝 Title: {_currentTune?.Title ?? "Untitled"}\n";
                    OutputMessages += $"🎼 Elements: {_currentTune?.Elements?.Count ?? 0}\n";
                    
                    if (result.Warnings.Count > 0)
                    {
                        OutputMessages += $"⚠️ Warnings:\n";
                        foreach (var warning in result.Warnings)
                        {
                            OutputMessages += $"   • {warning}\n";
                        }
                    }

                    StatusText = "Parsed successfully";
                }
                else
                {
                    _currentTune = null;
                    OutputMessages += $"❌ Parsing failed:\n";
                    foreach (var error in result.Errors)
                    {
                        OutputMessages += $"   • {error}\n";
                    }
                    StatusText = "Parsing failed";
                }

                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                OutputMessages += $"❌ Error during parsing: {ex.Message}\n";
                StatusText = "Error occurred";
                _logger?.LogError(ex, "Error during ABC parsing");
            }
        }

        private bool CanExecuteConvert()
        {
            return _currentTune != null && !IsConverting;
        }

        private async void ExecuteConvert()
        {
            if (_currentTune == null) return;

            try
            {
                IsConverting = true;
                StatusText = "Converting to MIDI...";

                // Get save file dialog (in real implementation)
                string outputPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                    $"{_currentTune.Title ?? "converted_tune"}.mid"
                );

                OutputMessages += $"🎹 Converting to MIDI...\n";
                OutputMessages += $"📁 Output: {outputPath}\n";

                var result = await _converter.ConvertToMidiFileAsync(_currentTune, outputPath);

                if (result.IsSuccess)
                {
                    OutputMessages += $"✅ Conversion successful!\n";
                    OutputMessages += $"📦 File size: {result.FileSizeBytes:N0} bytes\n";
                    OutputMessages += $"📍 Saved to: {result.OutputPath}\n";
                    StatusText = "Conversion completed";
                }
                else
                {
                    OutputMessages += $"❌ Conversion failed:\n";
                    foreach (var error in result.Errors)
                    {
                        OutputMessages += $"   • {error}\n";
                    }
                    StatusText = "Conversion failed";
                }

                if (result.HasWarnings)
                {
                    OutputMessages += $"⚠️ Warnings:\n";
                    foreach (var warning in result.Warnings)
                    {
                        OutputMessages += $"   • {warning}\n";
                    }
                }
            }
            catch (Exception ex)
            {
                OutputMessages += $"❌ Error during conversion: {ex.Message}\n";
                StatusText = "Error occurred";
                _logger?.LogError(ex, "Error during MIDI conversion");
            }
            finally
            {
                IsConverting = false;
            }
        }

        private void ExecuteClear()
        {
            AbcText = "";
            OutputMessages = "";
            StatusText = "Ready";
            _currentTune = null;
        }

        private void ExecuteLoadSample()
        {
            LoadSample();
        }

        #endregion

        private void LoadSample()
        {
            AbcText = @"X:1
T:Twinkle Twinkle Little Star
C:Traditional
M:4/4
L:1/4
K:C
C C G G | A A G2 | F F E E | D D C2 |
G G F F | E E D2 | G G F F | E E D2 |
C C G G | A A G2 | F F E E | D D C2 |";

            OutputMessages = "📄 Sample ABC notation loaded.\n";
            StatusText = "Sample loaded";
        }
    }

    /// <summary>
    /// Simple relay command implementation
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }
    }
}
