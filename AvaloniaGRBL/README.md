# AvaloniaGRBL

A cross-platform port of LaserGRBL using Avalonia UI Framework.

## Overview

AvaloniaGRBL is the multi-platform version of LaserGRBL, designed to run on Windows, Linux, and macOS. This project uses the [Avalonia UI Framework](https://avaloniaui.net/) which provides a modern, XAML-based UI framework for cross-platform .NET applications.

## Current Status

**Initial UI Implementation** - The application currently provides a UI mockup that matches the layout of the original LaserGRBL application. The buttons and controls are for visual reference only and do not have functionality yet.

### Implemented UI Elements

- **Menu Bar**: Complete menu structure matching LaserGRBL
  - Grbl menu (Connect, Disconnect, Configuration, etc.)
  - File menu (Open, Save, Send, etc.)
  - Generate menu (Power tests, accuracy tests, etc.)
  - Schema menu (Color themes)
  - Preview menu (Display options)
  - Language menu
  - Tools menu
  - Help menu

- **Status Bar**: Bottom status bar with placeholders for:
  - Line counters
  - Buffer status with progress bar
  - Estimated time
  - Current status

- **Split View Layout**:
  - **Left Panel**: Connection controls and Jog controls
    - COM port and baud rate selection
    - Connect button
    - Directional jog buttons (8-way + home)
    - Speed and step size controls
  - **Right Panel**: G-Code preview area
    - Toolbar with basic operation buttons
    - Canvas for G-Code visualization (placeholder)

## Building and Running

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- Avalonia templates (automatically installed with the project)

### Build Instructions

```bash
cd AvaloniaGRBL/AvaloniaGRBL
dotnet restore
dotnet build
```

### Run the Application

```bash
cd AvaloniaGRBL/AvaloniaGRBL
dotnet run
```

## Project Structure

```
AvaloniaGRBL/
├── AvaloniaGRBL/
│   ├── App.axaml              # Application definition
│   ├── App.axaml.cs           # Application code-behind
│   ├── Program.cs             # Entry point
│   ├── ViewLocator.cs         # MVVM view location logic
│   ├── Assets/                # Application resources (icons, images)
│   ├── ViewModels/            # MVVM ViewModels
│   │   ├── ViewModelBase.cs
│   │   └── MainWindowViewModel.cs
│   └── Views/                 # XAML views
│       ├── MainWindow.axaml
│       └── MainWindow.axaml.cs
└── README.md
```

## Development Roadmap

### Phase 1: UI Layout (✓ Completed)
- [x] Initialize Avalonia MVVM project
- [x] Create main window with menu bar
- [x] Implement split view layout
- [x] Add connection controls panel
- [x] Add jog controls panel
- [x] Add preview area panel
- [x] Add status bar

### Phase 2: Core Functionality (In Progress)
- [x] Implement serial port communication
- [x] Add GRBL connection logic
- [ ] Implement G-Code file loading
- [ ] Add G-Code preview rendering
- [ ] Implement jog controls functionality
- [ ] Add status monitoring

### Phase 3: Advanced Features (Planned)
- [ ] Image import and conversion
- [ ] Vector file support
- [ ] Configuration management
- [ ] Multi-language support
- [ ] Custom buttons
- [ ] WiFi connectivity support

## Technology Stack

- **Framework**: .NET 9.0
- **UI Framework**: Avalonia 11.3.9
- **Architecture**: MVVM (Model-View-ViewModel)
- **Package Manager**: NuGet

## Contributing

This is the initial implementation of the cross-platform version. Contributions are welcome! 

## License

This project inherits the GPLv3 license from LaserGRBL. See the main repository LICENSE.md for details.

## Screenshots

_Screenshots will be added as the application develops._

## Acknowledgments

- Based on [LaserGRBL](https://github.com/arkypita/LaserGRBL) by Diego Settimi
- Built with [Avalonia UI](https://avaloniaui.net/)
