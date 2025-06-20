# Anti-Idle Windows

A amateur, lightweight utility to prevent Windows from going idle or entering sleep mode.

## Features

- **3 Keep-alive methods**: Windows API, mouse jiggle, or hybrid
- **Interactive controls**: Pause/resume without closing the program
- **Configurable interval**: Set how often to keep the system awake
- **Single executable**: No dependencies required

## Usage

### Basic Usage
```bash
anti-idle-windows.exe
```

### Command Line Options
```bash
anti-idle-windows.exe [method] [interval]
```

**Methods:**
- `ExecutionState` - Uses Windows API (recommended, default)
- `MouseJiggle` - Simulates minimal mouse movement
- `Hybrid` - Combines both methods

**Examples:**
```bash
anti-idle-windows.exe ExecutionState 60    # Every 60 seconds
anti-idle-windows.exe MouseJiggle 30       # Mouse jiggle every 30s
anti-idle-windows.exe Hybrid 45            # Hybrid method every 45s
```

### Interactive Commands

Once running, use these commands:

- `p` or `pause` - Pause keep-alive (allows system to sleep)
- `r` or `resume` - Resume keep-alive
- `s` or `status` - Show current status
- `h` or `help` - Show help
- `q` or `quit` - Exit program

## Build

### Requirements
- .NET 8.0 SDK

### Build Single Executable
```bash
dotnet publish -c Release --self-contained true --runtime win-x64 /p:PublishSingleFile=true
```

The executable will be generated at:
```
bin\Release\net8.0\win-x64\publish\anti-idle-windows.exe
```

## How It Works

The program prevents Windows from entering sleep/idle mode by:

1. **ExecutionState**: Uses `SetThreadExecutionState` Windows API
2. **MouseJiggle**: Moves cursor 1 pixel and back
3. **Hybrid**: Combines both methods

The application runs the selected method at the specified interval (default: 30 seconds).

## License

This project is provided as-is for educational and utility purposes.


# Windows SmartScreen Warning

When running the executable, Windows may show a SmartScreen warning: "Windows protected your PC". This is normal for unsigned executables.

### Why This Happens
- The executable is not digitally signed with a code signing certificate
- Windows doesn't recognize the publisher
- Low download reputation (common for new releases)

### How to Run
1. Click "More info" on the SmartScreen dialog
2. Click "Run anyway"
3. The program will start normally

### Alternative: Build From Source
To avoid the warning entirely, compile the program yourself:

```bash
git clone https://github.com/yourusername/anti-idle-windows
cd anti-idle-windows
dotnet publish -c Release
```

The executable will be at `bin\Release\net8.0\win-x64\publish\anti-idle-windows.exe`

### Security Note
This project is open source - you can review the code before compiling. The SmartScreen warning doesn't indicate malware, just an unsigned executable.