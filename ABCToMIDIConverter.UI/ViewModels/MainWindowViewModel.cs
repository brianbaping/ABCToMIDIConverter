using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ABCToMIDIConverter.Core.Parsers;
using ABCToMIDIConverter.Core.Converters;
using ABCToMIDIConverter.Core.Models;
using Microsoft.Extensions.Logging;
using NAudio.Midi;

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
        private MidiOut? _midiOut;

        private string _abcText = "";
        private string _outputMessages = "";
        private string _statusText = "Ready";
        private bool _isConverting = false;
        private bool _isPlaying = false;
        private AbcTune? _currentTune;
        private string? _currentFilePath;
        private bool _hasUnsavedChanges = false;

        public MainWindowViewModel(ILogger<MainWindowViewModel>? logger = null)
        {
            _logger = logger;
            _parser = new AbcParser();
            _converter = new EnhancedMidiConverter();

            // Initialize MIDI output
            InitializeMidiOutput();

            // Initialize commands
            ParseCommand = new RelayCommand(ExecuteParse, CanExecuteParse);
            ConvertCommand = new RelayCommand(ExecuteConvert, CanExecuteConvert);
            PreviewCommand = new RelayCommand(ExecutePreview, CanExecutePreview);
            StopCommand = new RelayCommand(ExecuteStop, CanExecuteStop);
            ClearCommand = new RelayCommand(ExecuteClear);
            LoadSampleCommand = new RelayCommand(ExecuteLoadSample);
            
            // File management commands
            NewCommand = new RelayCommand(ExecuteNew);
            OpenCommand = new RelayCommand(ExecuteOpen);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            SaveAsCommand = new RelayCommand(ExecuteSaveAs, CanExecuteSaveAs);

            // Load recent files and settings
            LoadRecentFiles();
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
                    HasUnsavedChanges = true; // Mark as having unsaved changes
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

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (SetProperty(ref _isPlaying, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string? CurrentFilePath
        {
            get => _currentFilePath;
            set
            {
                if (SetProperty(ref _currentFilePath, value))
                {
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                if (SetProperty(ref _hasUnsavedChanges, value))
                {
                    OnPropertyChanged(nameof(WindowTitle));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string WindowTitle
        {
            get
            {
                var fileName = string.IsNullOrEmpty(CurrentFilePath) 
                    ? "Untitled" 
                    : Path.GetFileNameWithoutExtension(CurrentFilePath);
                
                var unsavedIndicator = HasUnsavedChanges ? "*" : "";
                return $"🎵 ABC to MIDI Converter - {fileName}{unsavedIndicator}";
            }
        }

        public ObservableCollection<string> RecentFiles { get; } = new();

        #endregion

        #region Commands

        public ICommand ParseCommand { get; }
        public ICommand ConvertCommand { get; }
        public ICommand PreviewCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand LoadSampleCommand { get; }
        
        // File management commands
        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }

        private bool CanExecuteParse()
        {
            return !string.IsNullOrWhiteSpace(AbcText) && !IsConverting;
        }

        private async void ExecuteParse()
        {
            try
            {
                StatusText = "Parsing...";
                OutputMessages = "🎵 Parsing ABC notation...\n";
                IsConverting = true; // Prevent multiple simultaneous parses

                // Run parsing on a background thread with timeout
                var result = await Task.Run(() => _parser.Parse(AbcText, timeoutSeconds: 30));

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
            catch (OperationCanceledException)
            {
                OutputMessages += $"⏱️ Parsing timed out after 30 seconds - file may be too complex\n";
                StatusText = "Parsing timed out";
                _logger?.LogWarning("ABC parsing timed out");
            }
            catch (Exception ex)
            {
                OutputMessages += $"❌ Error during parsing: {ex.Message}\n";
                StatusText = "Error occurred";
                _logger?.LogError(ex, "Error during ABC parsing");
            }
            finally
            {
                IsConverting = false;
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

        #region File Management Commands

        private void ExecuteNew()
        {
            if (HasUnsavedChanges && !ConfirmDiscardChanges())
                return;

            AbcText = "";
            OutputMessages = "";
            StatusText = "Ready";
            CurrentFilePath = null;
            HasUnsavedChanges = false;
            _currentTune = null;
            OutputMessages = "📄 New file created.\n";
        }

        private void ExecuteOpen()
        {
            if (HasUnsavedChanges && !ConfirmDiscardChanges())
                return;

            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "ABC files (*.abc)|*.abc|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    DefaultExt = "abc",
                    Title = "Open ABC File"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var content = File.ReadAllText(openFileDialog.FileName);
                    AbcText = content;
                    CurrentFilePath = openFileDialog.FileName;
                    HasUnsavedChanges = false;
                    
                    AddToRecentFiles(openFileDialog.FileName);
                    OutputMessages = $"📂 File opened: {Path.GetFileName(openFileDialog.FileName)}\n";
                    StatusText = "File loaded successfully";
                }
            }
            catch (Exception ex)
            {
                OutputMessages += $"❌ Error opening file: {ex.Message}\n";
                StatusText = "Error opening file";
                _logger?.LogError(ex, "Error opening ABC file");
            }
        }

        private bool CanExecuteSave()
        {
            return HasUnsavedChanges && !string.IsNullOrWhiteSpace(AbcText);
        }

        private void ExecuteSave()
        {
            if (string.IsNullOrEmpty(CurrentFilePath))
            {
                ExecuteSaveAs();
                return;
            }

            try
            {
                File.WriteAllText(CurrentFilePath, AbcText);
                HasUnsavedChanges = false;
                OutputMessages += $"💾 File saved: {Path.GetFileName(CurrentFilePath)}\n";
                StatusText = "File saved successfully";
            }
            catch (Exception ex)
            {
                OutputMessages += $"❌ Error saving file: {ex.Message}\n";
                StatusText = "Error saving file";
                _logger?.LogError(ex, "Error saving ABC file");
            }
        }

        private bool CanExecuteSaveAs()
        {
            return !string.IsNullOrWhiteSpace(AbcText);
        }

        private void ExecuteSaveAs()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "ABC files (*.abc)|*.abc|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    DefaultExt = "abc",
                    Title = "Save ABC File As",
                    FileName = GetSuggestedFileName()
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveFileDialog.FileName, AbcText);
                    CurrentFilePath = saveFileDialog.FileName;
                    HasUnsavedChanges = false;
                    
                    AddToRecentFiles(saveFileDialog.FileName);
                    OutputMessages += $"💾 File saved as: {Path.GetFileName(saveFileDialog.FileName)}\n";
                    StatusText = "File saved successfully";
                }
            }
            catch (Exception ex)
            {
                OutputMessages += $"❌ Error saving file: {ex.Message}\n";
                StatusText = "Error saving file";
                _logger?.LogError(ex, "Error saving ABC file");
            }
        }

        #endregion

        private bool CanExecutePreview()
        {
            return _currentTune != null && !IsConverting && !IsPlaying;
        }

        private async void ExecutePreview()
        {
            if (_currentTune == null) return;

            try
            {
                IsPlaying = true;
                StatusText = "Playing MIDI preview...";
                OutputMessages += $"🎵 Playing preview...\n";

                // Convert to MIDI in memory
                var converter = new MidiConverter();
                var midiEvents = converter.ConvertToMidi(_currentTune);

                // Create temporary file for playback
                var tempFile = Path.GetTempFileName() + ".mid";
                MidiFile.Export(tempFile, midiEvents);

                // Play the MIDI file
                await PlayMidiFileAsync(tempFile);

                // Clean up
                File.Delete(tempFile);

                OutputMessages += $"✅ Preview completed\n";
                StatusText = "Ready";
            }
            catch (Exception ex)
            {
                OutputMessages += $"❌ Preview error: {ex.Message}\n";
                StatusText = "Preview failed";
                _logger?.LogError(ex, "Error during MIDI preview");
            }
            finally
            {
                IsPlaying = false;
            }
        }

        private bool CanExecuteStop()
        {
            return IsPlaying;
        }

        private void ExecuteStop()
        {
            try
            {
                _midiOut?.Reset();
                IsPlaying = false;
                StatusText = "Playback stopped";
                OutputMessages += $"⏹️ Playback stopped\n";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error stopping MIDI playback");
            }
        }

        #endregion

        private void InitializeMidiOutput()
        {
            try
            {
                if (MidiOut.NumberOfDevices > 0)
                {
                    _midiOut = new MidiOut(0); // Use first available MIDI device
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not initialize MIDI output device");
            }
        }

        private async Task PlayMidiFileAsync(string filePath)
        {
            // Simple playback implementation using Windows built-in MIDI playback
            try
            {
                // For Windows, we can use Media Player or a simple approach
                // This is a simplified version - in production you might want a more sophisticated player
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };

                using var process = System.Diagnostics.Process.Start(processInfo);
                
                // Wait for a reasonable playback time (estimate based on tune length)
                var estimatedDurationMs = EstimatePlaybackDuration(_currentTune);
                var checkInterval = 500; // Check every 500ms
                var elapsed = 0;

                while (elapsed < estimatedDurationMs && IsPlaying)
                {
                    await Task.Delay(checkInterval);
                    elapsed += checkInterval;
                }

                // Stop the process if it's still running
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error during MIDI file playback");
                throw;
            }
        }

        private int EstimatePlaybackDuration(AbcTune? tune)
        {
            if (tune?.Elements == null) return 5000; // Default 5 seconds

            // Calculate approximate duration based on notes and tempo
            double totalDuration = 0;
            foreach (var element in tune.Elements)
            {
                if (element is Note note)
                    totalDuration += note.Duration;
                else if (element is Rest rest)
                    totalDuration += rest.Duration;
            }

            // Convert to milliseconds based on tempo and unit note length
            var beatsPerMinute = tune.Tempo;
            var secondsPerBeat = 60.0 / beatsPerMinute;
            var totalSeconds = totalDuration * secondsPerBeat / tune.UnitNoteLength;

            return Math.Max(2000, (int)(totalSeconds * 1000)); // Minimum 2 seconds
        }

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

        private bool ConfirmDiscardChanges()
        {
            if (!HasUnsavedChanges)
                return true;

            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to discard them?",
                "Unsaved Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }

        private void AddToRecentFiles(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            // Remove if already exists
            for (int i = RecentFiles.Count - 1; i >= 0; i--)
            {
                if (string.Equals(RecentFiles[i], filePath, StringComparison.OrdinalIgnoreCase))
                {
                    RecentFiles.RemoveAt(i);
                }
            }

            // Add to beginning
            RecentFiles.Insert(0, filePath);

            // Keep only the most recent 10 files
            while (RecentFiles.Count > 10)
            {
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
            }

            // Save to settings or registry if needed
            // TODO: Implement persistent storage for recent files
        }

        private string GetSuggestedFileName()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"ABCTune_{timestamp}.abc";
        }

        private void LoadRecentFiles()
        {
            // TODO: Load recent files from persistent storage
            // For now, just initialize with an empty collection
            RecentFiles.Clear();
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
