# Serial Port Communication Implementation

This document describes the serial port communication and GRBL connection implementation in AvaloniaGRBL.

## Architecture

The serial port communication is implemented using a layered architecture:

### 1. Services Layer

#### ISerialCommunication Interface
Defines the contract for serial port communication:
- `Configure(string portName, int baudRate)` - Configure port parameters
- `Open()` - Open the serial port
- `Close()` - Close the serial port
- `Write()` - Send data (byte, byte array, or string)
- `ReadLine()` - Read a line of data (blocking)
- `IsOpen` - Check if port is open
- `HasData` - Check if data is available

#### SerialPortCommunication Class
Cross-platform implementation using `System.IO.Ports.SerialPort`:
- Handles port opening/closing with proper error handling
- Configures standard GRBL parameters (8N1, no handshake)
- Manages buffer clearing on connect

#### SerialPortService Class
Static utility class providing:
- `GetAvailablePorts()` - Enumerate available serial ports
- `GetCommonBaudRates()` - Return standard baud rates for GRBL (115200, 57600, etc.)

#### GrblConnection Class
High-level GRBL connection manager:
- Manages connection lifecycle (connect/disconnect)
- Implements background receive loop for incoming data
- Provides event-driven architecture for status updates and data reception
- Handles GRBL soft reset on connection
- Thread-safe operation using async/await and cancellation tokens

### 2. ViewModel Layer

#### MainWindowViewModel
Manages UI state and user interactions:
- Port selection and baud rate configuration
- Connect/Disconnect commands
- Real-time connection log display
- Status text updates
- Thread-safe event handling using Avalonia Dispatcher

### 3. UI Layer

#### Connection Controls
Located in the left panel:
- COM Port dropdown (auto-populated with available ports)
- Baud Rate dropdown (standard GRBL rates)
- Connect/Disconnect button (text changes based on state)
- Refresh ports button

#### Status Display
- Status bar shows current connection status
- Connection log appears in the preview area when connected
- Timestamps on all log entries

## Usage

### Connecting to GRBL Device

1. Select a COM port from the dropdown
2. Select the appropriate baud rate (default: 115200)
3. Click "Connect"
4. Monitor the connection log for status updates

### Disconnecting

1. Click "Disconnect" button
2. Port will be closed and resources released

### Using Jog Controls

1. Ensure connected to a GRBL device
2. Set desired jog speed (mm/min) in the Speed field
3. Select step size from dropdown (0.1, 1, 10, or 100 mm)
4. Click directional buttons to move:
   - Arrow buttons for N, S, E, W, NE, NW, SE, SW movements
   - Home button (⌂) to run homing cycle
5. Monitor position in the status display and connection log

### Monitoring Status

- **Real-time Updates**: Status is automatically polled every 250ms when connected
- **Machine Position**: Shows absolute machine coordinates (MPos)
- **Work Position**: Shows position relative to work coordinate system (WPos)
- **Machine State**: Displays current GRBL state (Idle, Run, Hold, Alarm, etc.)
- **Status Bar**: Shows connection status and machine state
- **Left Panel**: Displays detailed position information

## Features

### Implemented
- ✓ Cross-platform serial port enumeration
- ✓ Serial port connection/disconnection
- ✓ GRBL soft reset on connect
- ✓ Background receive loop for incoming data
- ✓ Event-driven data reception
- ✓ Thread-safe UI updates
- ✓ Connection logging with timestamps
- ✓ Error handling and reporting
- ✓ GRBL status polling (? command) - automatic polling every 250ms
- ✓ Real-time position tracking - machine and work positions
- ✓ Status report parsing - extracts state, positions, feed rate
- ✓ Jog command support - $J= format with directional movement

### Future Enhancements
- Command queue management
- Alarm handling
- GRBL configuration reading ($$ command)

## Technical Notes

### Thread Safety
- Background receive loop runs on separate thread
- All UI updates are marshaled to UI thread using `Dispatcher.UIThread.Post()`
- Connection state changes are thread-safe

### Error Handling
- Port opening errors are caught and displayed
- Receive loop handles timeouts gracefully
- Connection errors trigger automatic cleanup

### GRBL Protocol
- Soft reset (Ctrl-X) sent on connection
- Line termination: `\n`
- Standard serial configuration: 8N1, no flow control

## Dependencies

- `System.IO.Ports` (v10.0.1) - Cross-platform serial port support
- `Avalonia` - UI framework
- `CommunityToolkit.Mvvm` - MVVM helpers

## Compatibility

- Windows: Native SerialPort support
- Linux: Requires appropriate permissions for `/dev/tty*` devices
- macOS: Native SerialPort support

### Linux Permissions

To use serial ports on Linux without root:
```bash
sudo usermod -a -G dialout $USER
# Log out and back in for changes to take effect
```
