# KVM Software Switch - Comprehensive Documentation

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Setup Guide](#setup-guide)
3. [Hardware Requirements](#hardware-requirements)
4. [Software Requirements](#software-requirements)
5. [Configuration](#configuration)
6. [Usage Guide](#usage-guide)
7. [Troubleshooting](#troubleshooting)
8. [Known Issues](#known-issues)
9. [Testing](#testing)
10. [Development](#development)
11. [Installer Package](installer.md)

## Architecture Overview

See [Architecture Documentation](../architecture.md) for detailed system architecture.

### Key Components

- **Host Control Service**: Windows service managing peripheral routing and endpoint sessions
- **WPF Dashboard**: Desktop application providing UI for endpoint management and video preview
- **Video Capture Pipeline**: DirectShow/Media Foundation integration for HDMI/USB-C capture
- **Peripheral Router**: USB-over-IP and USB-C HID redirection engine
- **Device Discovery**: Automatic detection of USB-C and HDMI capture devices
- **Hotkey Manager**: Global hotkey registration and handling

## Setup Guide

### Prerequisites

1. Windows 10/11 (64-bit)
2. Administrator privileges (required for USB device access and global hotkeys)
3. .NET 8.0 Runtime installed
4. USB-C or HDMI capture hardware

### Installation Steps

1. **Obtain the Installer**
   - Build or download `AuroraSwitchSetup.exe` (see [`docs/wiki/installer.md`](installer.md))
   - Run `AuroraSwitchSetup.exe` as Administrator

2. **Install Host Service**
   - The installer registers the Windows service automatically
   - Service name: "AuroraSwitch Host Service"
   - Service runs under LocalSystem account

3. **Launch Dashboard**
   - Start "KVM Software Switch" from Start Menu
   - Application will request administrator privileges

4. **Initial Configuration**
   - Connect your secondary machines (HP Laptop, MacBook Air)
   - Click "Refresh Devices" in the menu
   - Assign hotkeys to each endpoint (default: Ctrl+F1 for host, Ctrl+F2+ for endpoints)

### First-Time Setup for MacBook Air

If using MacBook Air with DisplayLink for multi-monitor support:

1. **Install DisplayLink Manager on MacBook Air**
   - Download from: https://www.displaylink.com/downloads
   - Install and restart MacBook Air

2. **Connect DisplayLink Dock**
   - Connect DisplayLink-compatible USB-C dock to MacBook Air
   - Connect HDMI capture devices to dock outputs
   - Connect dock to MacBook Air via USB-C

3. **Verify Detection**
   - Launch KVM Software Switch on host PC
   - Click "Refresh Devices"
   - Verify MacBook Air appears with DisplayLink dock detected

## Hardware Requirements

### Host PC Requirements

- **CPU**: Intel Core i5 or AMD Ryzen 5 (or better)
- **RAM**: 8GB minimum (16GB recommended)
- **Storage**: 500MB free space
- **USB Ports**: USB 3.0+ for USB-C connections
- **Network**: Gigabit Ethernet (for USB/IP support)

### Capture Hardware

**For HDMI Capture:**
- USB 3.0 HDMI capture card (e.g., Elgato Cam Link, AVerMedia Live Gamer Ultra)
- OR PCIe HDMI capture card for lower latency
- HDMI cables from secondary machines to capture cards

**For USB-C Direct Connection:**
- USB-C to USB-C cable (USB 3.1 Gen 2 or Thunderbolt 3/4)
- USB-C ports on both host and secondary machines

**For Multi-Monitor MacBook Air:**
- DisplayLink-compatible USB-C dock (e.g., Dell D6000, Plugable UD-6950H)
- Multiple HDMI capture cards (one per DisplayLink output)
- DisplayLink Manager software on MacBook Air

### Secondary Machine Requirements

**HP Laptop:**
- USB-C port with DisplayPort Alt Mode support
- Windows 10/11 recommended

**MacBook Air M2:**
- USB-C port (Thunderbolt 3/4)
- macOS 12+ recommended
- DisplayLink Manager (for multi-monitor support)

## Software Requirements

### Host PC

- Windows 10 (version 1903+) or Windows 11
- .NET 8.0 Runtime
- DirectX 11 compatible graphics card
- Administrator privileges

### Secondary Machines

**Windows Machines:**
- Windows 10/11
- No additional software required (for USB-C direct connection)
- USB/IP agent (optional, for network-based connection)

**macOS Machines:**
- macOS 12+ (Monterey or later)
- DisplayLink Manager (for multi-monitor support)
- USB/IP agent (optional, for network-based connection)

## Configuration

### Endpoint Configuration

Endpoints are automatically discovered, but can be manually configured:

1. **Access Settings**
   - Menu → Settings → Endpoints

2. **Edit Endpoint**
   - Select endpoint from list
   - Modify name, connection type, or device IDs
   - Save changes

3. **Hotkey Assignment**
   - Menu → Endpoints → Manage Hotkeys
   - Select endpoint
   - Press desired hotkey combination
   - Click "Assign"

### Default Hotkeys

- **Ctrl+F1**: Switch to Host PC
- **Ctrl+F2**: Switch to first secondary machine
- **Ctrl+F3**: Switch to second secondary machine
- **Ctrl+Shift+F12**: Emergency return to host (always works)

### Configuration Files

Configuration is stored in:
- `%LocalAppData%\KvmSwitch\`
  - `endpoints.db`: SQLite database of endpoints
  - `hotkeys.json`: Hotkey mappings
  - `routing.json`: Routing configuration

## Usage Guide

### Basic Operation

1. **Start Application**
   - Launch "KVM Software Switch" from Start Menu
   - Grant administrator privileges when prompted

2. **Select Endpoint**
   - Click on endpoint in the left panel
   - OR press assigned hotkey (e.g., Ctrl+F2)

3. **Control Secondary Machine**
   - Mouse and keyboard input automatically routes to selected endpoint
   - Video feed displays in preview window (if capture device configured)
   - Audio routes to host speakers (if capture device supports audio)

4. **Switch Between Machines**
   - Press hotkey for desired endpoint
   - OR click endpoint in list
   - Control switches immediately

### Advanced Features

**Multi-Monitor Support (MacBook Air)**
- Requires DisplayLink-compatible dock
- Connect multiple HDMI capture devices to dock outputs
- Application automatically detects and maps multiple displays
- Each display appears as separate video feed (future enhancement)

**Video Preview**
- Select endpoint to view its video feed
- Video updates in real-time
- Supports up to 1080p60 (depending on capture device)

**Audio Routing**
- Audio from active endpoint plays through host speakers
- Mute/unmute per endpoint via context menu
- Volume control per endpoint (future enhancement)

## Troubleshooting

### Device Not Detected

**Symptoms**: Secondary machine does not appear in endpoint list

**Solutions**:
1. Verify USB-C cable is connected and supports data transfer
2. Click "Refresh Devices" in menu
3. Check Windows Device Manager for USB device recognition
4. Verify USB-C port supports DisplayPort Alt Mode
5. Try different USB-C port or cable

### Hotkey Not Working

**Symptoms**: Pressing hotkey does not switch endpoints

**Solutions**:
1. Verify application has administrator privileges
2. Check if hotkey conflicts with other applications
3. Reassign hotkey via Menu → Endpoints → Manage Hotkeys
4. Restart application
5. Verify Windows service is running

### Video Not Displaying

**Symptoms**: Video preview is black or not showing

**Solutions**:
1. Verify HDMI capture device is connected and recognized
2. Check capture device drivers are installed
3. Verify HDMI cable is connected from secondary machine to capture device
4. Try different HDMI port on capture device
5. Check capture device is not used by another application

### High Latency

**Symptoms**: Mouse/keyboard input feels laggy

**Solutions**:
1. For USB/IP: Verify Gigabit Ethernet connection (not WiFi)
2. Check network congestion
3. For USB-C: Verify USB 3.1 Gen 2 or Thunderbolt connection
4. Close other resource-intensive applications
5. Check CPU usage (should be < 15%)

### Application Freezing or Timing Out

**Symptoms**: Application freezes when starting, switching endpoints, or refreshing devices. Timeout errors when connecting to endpoints.

**Root Cause**: Blocking async operations and lack of timeout handling in connection attempts.

**Solutions** (Fixed in latest version):
1. Application now uses timeout-protected async operations (5-10 second timeouts)
2. HDMI-only endpoints no longer require USB connection (video capture works independently)
3. Connection failures gracefully fall back to video-only mode when possible
4. Database operations are now fully async to prevent UI blocking
5. USB-C endpoints with HDMI capture devices can work in video-only mode

**Status**: Fixed in version 1.0.1+ - Application should no longer freeze or timeout indefinitely.

### MacBook Air Multi-Monitor Not Working

**Symptoms**: Only one display active on MacBook Air

**Solutions**:
1. Verify DisplayLink Manager is installed on MacBook Air
2. Restart MacBook Air after DisplayLink installation
3. Verify DisplayLink-compatible dock is connected
4. Check dock supports multiple displays
5. Verify capture devices are connected to dock outputs

### Exclusive Control Not Working

**Symptoms**: Input goes to multiple machines simultaneously

**Solutions**:
1. Verify only one endpoint is active at a time
2. Check endpoint status in UI (should show "Active" for one only)
3. Restart application
4. Verify Windows service is running correctly
5. Check for conflicting USB drivers

- ### .NET SDK Missing / `dotnet` Command Not Found
  - **Symptoms**: Running `dotnet` or `dotnet run` returns "`The application 'run' does not exist`" along with a notice that no .NET SDKs were found.
  - **Root Cause**: The .NET SDK (version 8.0 or later) is not installed or is not available on the system `PATH`.
  - **Resolution**:
    1. Install the .NET SDK via [https://aka.ms/dotnet/download](https://aka.ms/dotnet/download) (select x64 installer for Windows 10/11).
    2. Or run `winget install Microsoft.DotNet.SDK.8` in an elevated PowerShell session.
    3. Restart the PowerShell window (or sign out/in) so the updated `PATH` is picked up.
    4. Verify installation with `dotnet --info` before building (`dotnet restore`, `dotnet build`).

## Known Issues

### Issue #1: DisplayLink Latency
**Description**: Multi-monitor via DisplayLink adds 20-50ms latency compared to native displays.

**Workaround**: Acceptable for most use cases, but may be noticeable for gaming or high-precision tasks.

**Status**: Expected behavior, limitation of DisplayLink technology.

---

### Issue #2: macOS Multi-Monitor Limitation
**Description**: M2 MacBook Air natively supports only one external display.

**Workaround**: Use DisplayLink-compatible dock for multi-monitor support.

**Status**: macOS limitation, not a bug in KVM software.

---

### Issue #3: USB/IP Network Dependency
**Description**: USB/IP requires stable Gigabit LAN for acceptable latency.

**Workaround**: Use wired Gigabit Ethernet connection, avoid WiFi.

**Status**: Network limitation, expected behavior.

---

### Issue #4: Capture Device Compatibility
**Description**: Some HDMI capture devices may not be detected correctly.

**Workaround**: Check capture device compatibility list, update drivers.

**Status**: Under investigation, driver-dependent.

---

### Issue #5: Hotkey Conflicts
**Description**: Some applications may intercept global hotkeys.

**Workaround**: Reassign hotkeys to avoid conflicts, close conflicting applications.

**Status**: Windows limitation, expected behavior.

---

### Issue #6: Application Timeouts and Freezing (FIXED)
**Description**: Application would freeze or timeout when connecting to endpoints, especially USB-C endpoints or when database operations were slow.

**Root Cause**: 
- Blocking `.Wait()` calls on async operations in UI thread
- No timeout handling for connection attempts
- USB-C endpoints failing entirely even when HDMI video was available
- Synchronous database operations blocking the UI

**Resolution** (v1.0.1+):
- Added timeout protection (5-10 seconds) for all async operations
- Made database operations fully async (OpenAsync, ExecuteNonQueryAsync, ExecuteReaderAsync)
- HDMI-only endpoints no longer require USB connection
- USB-C/Hybrid endpoints can work in video-only mode when USB connection fails
- Connection failures gracefully degrade to video-only mode when capture devices are available
- Improved error messages to distinguish between connection failures and video capture issues

**Status**: Fixed. Application should start and switch endpoints without freezing or timing out indefinitely.

## Testing

See [Testing Documentation](testing.md) for comprehensive test procedures and results.

## Development

### Building from Source

**Prerequisites**:
- Visual Studio 2022 or .NET 8.0 SDK
- Windows SDK
- Git

**Build Steps**:
```bash
git clone [repository-url]
cd "KVM Software Switch/src"
dotnet restore
dotnet build
```

### Project Structure

```
src/
├── KvmSwitch.Core/          # Core interfaces and models
├── KvmSwitch.Dashboard/      # WPF desktop application
├── KvmSwitch.HostService/    # Windows service
├── KvmSwitch.Capture/        # Video/audio capture
└── KvmSwitch.Routing/        # Peripheral routing
```

### Contributing

1. Fork the repository
2. Create feature branch
3. Make changes
4. Add tests
5. Submit pull request

### Installer Package

- Run `installer/build-installer.ps1` to generate the AuroraSwitch installer into `installer/dist`.
- See [`docs/wiki/installer.md`](installer.md) for end-to-end packaging and validation details.

## Support

For issues, questions, or contributions:
- GitHub Issues: [repository-url]/issues
- Documentation: See `docs/` directory
- Architecture: See `docs/architecture.md`

## License

[Specify license here]

## Changelog

### Version 1.0.0 (Initial Release)
- Basic USB-C and USB/IP peripheral routing
- Hotkey-based endpoint switching
- Video capture and preview
- Device discovery and endpoint management
- Multi-monitor support via DisplayLink (MacBook Air)

