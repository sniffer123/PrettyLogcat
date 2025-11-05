# PrettyLogcat

A modern Android Logcat viewer built with C# WPF and MaterialDesignInXamlToolkit.

## Features

- **Real-time Logcat Streaming**: Connect to Android devices and view logcat output in real-time
- **Material Design UI**: Beautiful and modern interface using MaterialDesignInXamlToolkit
- **Advanced Filtering**: Filter logs by level, tag, message content, and PID
- **Device Management**: Automatic device detection and connection management
- **File Operations**: Save logs to file and load existing log files
- **Performance Optimized**: Efficient log processing and UI virtualization
- **Cross-platform**: Built on .NET 8 for Windows

## Prerequisites

- .NET 8.0 Runtime
- Android SDK Platform Tools (ADB)
- Windows 10/11

## Installation

1. Clone the repository:
```bash
git clone <repository-url>
cd PrettyLogcat
```

2. Build the project:
```bash
dotnet build
```

3. Run the application:
```bash
dotnet run --project PrettyLogcat
```

## Usage

### Connecting to a Device

1. Connect your Android device via USB with USB Debugging enabled
2. Select the device from the dropdown in the toolbar
3. Click the "Connect" button
4. Logcat output will start streaming automatically

### Filtering Logs

- **Log Levels**: Use checkboxes to show/hide different log levels (Verbose, Debug, Info, Warning, Error, Fatal)
- **Tag Filter**: Enter text to filter by log tag
- **Message Filter**: Enter text to filter by log message content
- **PID Filter**: Enter a process ID to filter by specific process

### File Operations

- **Save Logs**: Click "Save Logs" to export current filtered logs to a file
- **Open File**: Click "Open File" to load and view existing log files

### Additional Features

- **Auto Scroll**: Toggle automatic scrolling to follow new log entries
- **Word Wrap**: Toggle text wrapping for long log messages
- **Clear Logs**: Clear current log display and device logcat buffer

## Architecture

The application follows MVVM pattern with dependency injection:

- **Models**: `LogEntry`, `AndroidDevice` - Data models
- **Services**: 
  - `AdbService` - ADB command execution and device communication
  - `LogcatService` - Log parsing and streaming
  - `DeviceService` - Device management and monitoring
  - `FilterService` - Log filtering logic
  - `FileService` - File operations
- **ViewModels**: `MainViewModel` - UI logic and data binding
- **Views**: `MainWindow` - WPF UI with Material Design

## Dependencies

- **MaterialDesignThemes** - Material Design UI components
- **MaterialDesignColors** - Material Design color palette
- **Microsoft.Extensions.DependencyInjection** - Dependency injection
- **Microsoft.Extensions.Logging** - Logging framework
- **Newtonsoft.Json** - JSON serialization
- **System.Reactive** - Reactive extensions for async streams

## Performance Features

- **UI Virtualization**: Efficient rendering of large log lists
- **Async Processing**: Non-blocking log processing and device operations
- **Memory Management**: Automatic cleanup and disposal of resources
- **Batch Processing**: Efficient handling of high-volume log streams

## Troubleshooting

### ADB Not Found
- Install Android SDK Platform Tools
- Add ADB to your system PATH
- Restart the application

### Device Not Detected
- Enable USB Debugging on your Android device
- Accept the USB debugging prompt on your device
- Try different USB cables/ports
- Run `adb devices` in command line to verify ADB connection

### Performance Issues
- Reduce the number of visible log levels
- Use specific filters to reduce log volume
- Clear logs periodically during long sessions

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- MaterialDesignInXamlToolkit for the beautiful UI components
- Android Debug Bridge (ADB) for device communication
- .NET team for the excellent framework