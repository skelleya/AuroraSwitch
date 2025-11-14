# Quick Start Guide

## First-Time Setup

### Prerequisites
- Windows 10/11 (64-bit)
- Administrator privileges
- .NET 8.0 Runtime
- USB-C or HDMI capture hardware

### Installation Steps

1. **Build the Solution**
   ```bash
   cd src
   dotnet --info
   dotnet restore
   dotnet build --configuration Release
   ```
   - If `dotnet --info` fails, install the .NET SDK 8.0 from https://aka.ms/dotnet/download or run `winget install Microsoft.DotNet.SDK.8` in an elevated PowerShell window.

2. **Run as Administrator**
   - Right-click `KvmSwitch.Dashboard.exe` → Run as Administrator
   - Grant administrator privileges when prompted

3. **Connect Your Devices**
   - Connect HP Laptop via USB-C
   - Connect MacBook Air via USB-C
   - Connect HDMI capture devices (if using HDMI)

4. **Initial Configuration**
   - Launch the application
   - Click "Refresh Devices" in the menu
   - Endpoints will be auto-discovered
   - Default hotkeys will be assigned automatically:
     - **Ctrl+F1**: Switch to Host PC
     - **Ctrl+F2**: Switch to first secondary machine
     - **Ctrl+F3**: Switch to second secondary machine
     - **Ctrl+Shift+F12**: Emergency return to host

5. **Test Switching**
   - Press Ctrl+F2 to switch to first secondary machine
   - Move mouse and type - input should go to secondary machine
   - Press Ctrl+F1 to return to host
   - Verify host receives input again

## Default Hotkeys

The system automatically assigns hotkeys on first launch:

| Hotkey | Action |
|--------|--------|
| Ctrl+F1 | Switch to Host PC |
| Ctrl+F2 | Switch to first endpoint |
| Ctrl+F3 | Switch to second endpoint |
| ... | ... |
| Ctrl+F10 | Switch to ninth endpoint |
| Ctrl+Shift+F12 | Emergency return to host (always works) |

## Troubleshooting

### Hotkeys Not Working
- Ensure application is running as Administrator
- Check if hotkeys conflict with other applications
- Restart the application

### Devices Not Detected
- Verify USB-C cables support data transfer
- Check Windows Device Manager for device recognition
- Click "Refresh Devices" in the menu
- Ensure devices are powered on

### Input Not Routing
- Verify endpoint is marked as "Active" in the UI
- Check USB-C connection is stable
- Ensure LibUsbDotNet drivers are installed
- Check application logs for errors

### Video Not Displaying
- Verify HDMI capture device is connected
- Check capture device drivers are installed
- Ensure capture device is not used by another application
- Try selecting a different endpoint and switching back

## Next Steps

- Customize hotkeys via Menu → Endpoints → Manage Hotkeys
- Configure endpoint names and settings
- Set up multi-monitor support for MacBook Air (requires DisplayLink)

