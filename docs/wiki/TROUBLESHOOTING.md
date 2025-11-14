# Troubleshooting Guide - AuroraSwitch Dashboard

## Dashboard Won't Launch

### Quick Fix: Launch the Dashboard

The Dashboard is a standalone WPF application that does **NOT** require the HostService to be running. You can launch it directly:

#### Option 1: From Development Build
```powershell
# Navigate to the project directory
cd "G:\KVM Software Switch\src\KvmSwitch.Dashboard\bin\Debug\net8.0-windows"

# Run the executable
.\KvmSwitch.Dashboard.exe
```

#### Option 2: From Installed Location
If you've run the installer, the Dashboard should be at:
```
C:\Program Files\AuroraSwitch\Dashboard\KvmSwitch.Dashboard.exe
```

#### Option 3: Use the Launcher Script
Run the `launch-dashboard.ps1` script from the project root.

### Common Issues

#### 1. Application Crashes on Startup

**Symptoms**: Window appears briefly then closes, or error dialog appears.

**Solutions**:
- Check Windows Event Viewer (Applications and Services Logs) for detailed error messages
- Ensure .NET 8.0 Runtime is installed
- Check that the database directory exists: `%LocalAppData%\KvmSwitch\`
- Verify all required DLLs are present in the same directory as the executable

**Database Location**:
```
%LocalAppData%\KvmSwitch\endpoints.db
```
(Usually: `C:\Users\<YourUsername>\AppData\Local\KvmSwitch\endpoints.db`)

#### 2. .NET Runtime Error: `The handle is invalid (0x80070006)`

**Symptoms**:
- `KvmSwitch.Dashboard.exe` closes immediately.
- Event Viewer shows `.NET Runtime` error 1026 or `Application Error` 1000 with exception code `0xe0434352`.
- Message references `BitmapSource.CriticalCopyPixels` or `IconHelper`.

**Root Cause**:
- Older builds loaded `icon.ico` directly from a stream, and the stream was disposed before WPF finished creating the window icon.

**Resolution**:
1. Pull the latest source (`git pull`) or reinstall the updated dashboard build.
2. Rebuild the dashboard: `dotnet build src\KvmSwitch.Dashboard\KvmSwitch.Dashboard.csproj -c Debug`.
3. Relaunch using `launch-dashboard.ps1` or run the EXE directly.
4. Verify the Event Viewer no longer records the `.NET Runtime` crash.

> The fix uses `IconBitmapDecoder` with `BitmapCacheOption.OnLoad`, so the icon is fully loaded before the stream closes.

#### 3. Window Doesn't Appear / Hidden in System Tray

**Symptoms**:
- Task Manager shows `KvmSwitch.Dashboard.exe`, but no window is visible.
- Nothing appears on the taskbar.

**Solutions**:
- Look for the **AuroraSwitch** tray icon near the clock. If hidden, click the `^` caret to expand hidden icons.
- Double-click the tray icon or right-click → **Show Dashboard** to restore the window.
- If the window was minimized, it hides to the tray by design (no taskbar icon while minimized).
- To exit completely, right-click the tray icon → **Exit** or close the window when it is visible.

#### 3b. Process Runs but No Window or Tray Icon Ever Shows

**Symptoms**:
- Task Manager shows `KvmSwitch.Dashboard.exe` with `MainWindowHandle = 0`.
- Tray icon never appears even after waiting 30+ seconds.
- `C:\ProgramData\AuroraSwitch\logs\dashboard-launch.out.log` stops at `HID input interceptor initialized`.

**Root Cause** (builds prior to 2025-11-14):
- `AppSettingsService` loaded `settings.json` using `await` while the UI thread was blocked with `.GetAwaiter().GetResult()`, causing a deadlock before the shell window was ever created.

**Resolution**:
1. Pull the latest source or install the updated build (version ≥ `1.2.6`) which applies `ConfigureAwait(false)` to the settings service.
2. Rebuild: `dotnet build src\KvmSwitch.sln -c Release`.
3. Reinstall via `AuroraSwitchSetup.exe` or copy the rebuilt `KvmSwitch.Dashboard.exe` to the install directory.

**Verification**:
- Relaunch the dashboard and confirm the log now continues with `Dashboard initialized successfully` and `MainWindow created - application ready`.
- `Get-Process KvmSwitch.Dashboard | Select MainWindowHandle` should now report a non-zero handle once the window is shown.

#### 4. MacBook Not Detected over USB-C

**Symptoms**:
- MacBook is plugged in via USB-C → USB-A adapter but no endpoint appears after "Refresh Devices".
- Event log shows no new `USB\VID_05AC` entries.

**Solutions**:
- Use **Endpoints > Refresh Devices** after connecting the cable. The updated device discovery routine reads `Win32_PnPEntity` entries so vendor IDs such as `VID_05AC` are captured even through passive adapters.
- Confirm the Apple USB device shows up in Device Manager under **Universal Serial Bus devices**.
- If still missing, toggle the cable (unplug/replug) and check `%LOCALAPPDATA%\KvmSwitch\endpoints.db` for new records.
- As a fallback, run the Dashboard as administrator to allow WMI to enumerate USB descriptors without access errors.

#### 5. "Cannot find dotnet" Error

**Symptoms**: Error about .NET runtime not found.

**Solutions**:
- Install .NET 8.0 Desktop Runtime from: https://dotnet.microsoft.com/download/dotnet/8.0
- Ensure the runtime matches the architecture (x64)

#### 6. Database Errors

**Symptoms**: Errors about SQLite or database access.

**Solutions**:
- Ensure you have write permissions to `%LocalAppData%\KvmSwitch\`
- Delete the database file and let it recreate: `%LocalAppData%\KvmSwitch\endpoints.db`
- Check disk space availability

#### 5. Missing Dependencies

**Symptoms**: "DLL not found" errors.

**Solutions**:
- Rebuild the solution: `dotnet build`
- Ensure all NuGet packages are restored: `dotnet restore`
- Check that all DLLs are in the output directory

### Debugging Steps

1. **Run from Command Line** to see error messages:
   ```powershell
   cd "G:\KVM Software Switch\src\KvmSwitch.Dashboard\bin\Debug\net8.0-windows"
   .\KvmSwitch.Dashboard.exe
   ```

2. **Check Event Viewer**:
   - Open Event Viewer
   - Navigate to: Windows Logs > Application
   - Look for errors from "AuroraSwitch" or ".NET Runtime"

3. **Enable Console Logging**:
   The Dashboard logs to console by default. If running from command line, you should see initialization messages.

4. **Check Database**:
   ```powershell
   # Check if database directory exists
   Test-Path "$env:LOCALAPPDATA\KvmSwitch"
   
   # Check if database file exists
   Test-Path "$env:LOCALAPPDATA\KvmSwitch\endpoints.db"
   ```

### Architecture Notes

**Important**: The Dashboard is a **standalone application** that:
- Does NOT require the HostService to be running
- Creates its own database in `%LocalAppData%\KvmSwitch\endpoints.db`
- Initializes all services internally (EndpointRegistry, HotkeyManager, etc.)
- Can run independently for configuration and testing

The HostService is optional and provides:
- Background hotkey handling
- Device discovery
- Peripheral routing
- But the Dashboard can function without it for basic configuration

### Getting Help

If the Dashboard still won't launch:

1. Capture the exact error message (screenshot or copy text)
2. Check Event Viewer for detailed logs
3. Note what happens when you try to launch:
   - Does anything appear?
   - Does it crash immediately?
   - Does the process start but no window?
4. Check the console output if running from command line

### Manual Configuration

If you need to configure things without the Dashboard:

1. **Database Location**: `%LocalAppData%\KvmSwitch\endpoints.db`
2. **Hotkey Registry**: Stored in the same database
3. **Settings**: Currently stored in the database (future: `%AppData%\Roaming\KvmSwitch\settings.yaml`)

You can use SQLite tools to inspect/modify the database if needed.
