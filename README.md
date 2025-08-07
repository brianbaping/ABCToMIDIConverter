# ABC to MIDI Converter

A .NET 8 application that converts ABC notation music files to MIDI format, featuring both a core library and a WPF-based user interface.

## 📖 Overview

ABC notation is a shorthand form of musical notation that allows music to be written in plain text format. This application parses ABC notation files and converts them to MIDI files that can be played by any MIDI-compatible software or device.

## 🏗️ Project Structure

```
ABCToMIDIConverter/
├── ABCToMIDIConverter.Core/          # Core conversion library
│   ├── Converters/                   # MIDI conversion logic
│   ├── Models/                       # Data models for musical elements
│   ├── Parsers/                      # ABC notation parsing components
│   └── Utils/                        # Utility classes
├── ABCToMIDIConverter.UI/            # WPF user interface
├── ABCToMIDIConverter.Tests/         # Unit tests
└── ABCToMIDIConverter.sln            # Visual Studio solution file
```

### Key Components

- **ABCToMIDIConverter.Core**: The main library containing:
  - `AbcParser`: Parses ABC notation text into structured data
  - `MidiConverter`: Converts parsed ABC data to MIDI format
  - `Models`: Represents musical elements (notes, rests, time signatures, etc.)
  
- **ABCToMIDIConverter.UI**: WPF application providing a user-friendly interface

- **ABCToMIDIConverter.Tests**: Unit tests ensuring code quality and functionality

## 🚀 Features

- ✅ Parse ABC notation files
- ✅ Convert to standard MIDI format
- ✅ Support for musical elements:
  - Notes with various durations
  - Rests
  - Time signatures
  - Key signatures
  - Accidentals (sharps, flats, naturals)
- ✅ WPF-based graphical user interface
- ✅ Comprehensive unit testing
- ✅ Built on .NET 8 with modern C# features

## 🛠️ Prerequisites

- **.NET 8.0 SDK** or later
- **Visual Studio 2022** (recommended) or **Visual Studio Code**
- **Windows** (for WPF UI component)

## 📦 Dependencies

### Core Library
- **NAudio** (v2.2.1) - For MIDI file generation
- **Microsoft.Extensions.Logging.Abstractions** (v9.0.8) - For logging support

### UI Application
- **Microsoft.Extensions.DependencyInjection** (v9.0.8) - For dependency injection
- **Microsoft.Extensions.Logging** (v9.0.8) - For logging

## 🔧 Getting Started

### Clone and Build

```bash
git clone https://github.com/briabaping/ABCToMIDIConverter.git
cd ABCToMIDIConverter
dotnet build
```

### Running the Application

#### WPF User Interface
```bash
dotnet run --project ABCToMIDIConverter.UI
```

#### Using the Core Library
```csharp
using ABCToMIDIConverter.Core.Parsers;
using ABCToMIDIConverter.Core.Converters;

// Parse ABC notation
var parser = new AbcParser();
var parseResult = parser.Parse(abcContent);

if (parseResult.IsSuccess)
{
    // Convert to MIDI
    var converter = new MidiConverter();
    converter.ConvertToMidiFile(parseResult.Tune, "output.mid");
}
```

### Running Tests

```bash
dotnet test
```

## 📝 ABC Notation Example

```abc
X:1
T:Twinkle Twinkle Little Star
M:4/4
K:C
C C G G | A A G2 | F F E E | D D C2 |
G G F F | E E D2 | G G F F | E E D2 |
C C G G | A A G2 | F F E E | D D C2 |
```

## 🧪 Testing

The project includes comprehensive unit tests covering:
- ABC notation parsing
- MIDI conversion accuracy
- Error handling and edge cases
- Musical element validation

Run tests with detailed output:
```bash
dotnet test --verbosity normal
```

## 🎵 Supported ABC Features

- **Header Fields**: Title (T:), Reference (X:), Time Signature (M:), Key (K:), Composer (C:)
- **Note Durations**: Whole, half, quarter, eighth, sixteenth notes
- **Accidentals**: Sharp (#), Flat (b), Natural (=)
- **Rests**: Various duration rests
- **Time Signatures**: Common time signatures (4/4, 3/4, 2/4, etc.)
- **Key Signatures**: Major and minor keys

## 🚧 Roadmap

- [ ] Support for more complex ABC features (ornaments, dynamics)
- [ ] Chord notation support
- [ ] Multi-voice/part handling
- [ ] Real-time playback functionality
- [ ] Export to other formats (MusicXML, etc.)
- [ ] Command-line interface
- [ ] Cross-platform UI (Avalonia or MAUI)

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔗 Resources

- [ABC Notation Standard](http://abcnotation.com/)
- [NAudio Documentation](https://github.com/naudio/NAudio)
- [MIDI Specification](https://www.midi.org/specifications)

## 📧 Contact

For questions or support, please open an issue on GitHub.

---

*Built with ❤️ using .NET 8 and WPF*
