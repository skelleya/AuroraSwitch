# Quick Start Guide - AuroraSwitch Dashboard

## How to Launch the Dashboard

### Method 1: Use the Launcher Script (Easiest)
```powershell
cd "G:\KVM Software Switch"
.\launch-dashboard.ps1
```

### Method 2: Run Directly from Build Output
```powershell
cd "G:\KVM Software Switch\src\KvmSwitch.Dashboard\bin\Debug\net8.0-windows"
.\KvmSwitch.Dashboard.exe
```

### Method 3: From Start Menu (After Installation)
After running the installer, look for "AuroraSwitch Dashboard" in the Start Menu.

## What the Dashboard Does

The Dashboard is your **main control interface** for the KVM Software Switch. It allows you to:

1. **View Endpoints**: See all detected computers (Host PC, connected laptops, etc.)
2. **Switch Between Computers**: Click on an endpoint to switch keyboard/mouse control
3. **Configure Hotkeys**: Assign keyboard shortcuts for quick switching
4. **View Video Feeds**: See live video from capture devices (if configured)
5. **Manage Devices**: Refresh and discover new USB-C/HDMI devices

## First Launch

When you first launch the Dashboard:

1. **A window should appear** with:
   - Left panel: List of endpoints (should show "Host PC" by default)
   - Right panel: Video preview area (black until capture is configured)
   - Menu bar: File, Endpoints, Help menus
   - Status bar: Shows current status

2. **System tray behavior**:
   - The dashboard now minimizes to the Windows tray (near the clock).
   - Double-click the **AuroraSwitch** tray icon or right-click → **Show Dashboard** to bring it back.
   - Use the tray menu → **Exit** to shut it down completely.

3. **If the window still doesn't appear**:
   - Check Task Manager - is `KvmSwitch.Dashboard.exe` running?
   - Ensure you are on the latest build (`dotnet build` after `git pull`).
   - Check Event Viewer for `.NET Runtime` errors (see [Troubleshooting Guide](troubleshooting.md)).

3. **The Dashboard creates**:
   - Database: `%LocalAppData%\KvmSwitch\endpoints.db`
   - Default "Host PC" endpoint
   - Default hotkeys (if configured)

## Basic Usage

### Switching Between Computers

1. **Using the UI**:
   - Click on an endpoint in the left panel
   - The selected endpoint becomes active

2. **Using Hotkeys** (if configured):
   - Default hotkeys may be set up automatically
   - Use **Endpoints > Manage Hotkeys** to add/edit/remove shortcuts

### Configure Hotkeys

- Open **Endpoints > Manage Hotkeys**
- Click **Add** to pick modifiers, a key (F-keys, letters, numbers), and the target endpoint
- The dialog prevents duplicate combinations and stores mappings in the local SQLite database
- Use **Edit** to update an existing shortcut or **Remove** to clear it

### Refreshing Device List

- Go to: **Endpoints > Refresh Devices**
- This scans for new USB-C and HDMI capture devices
- The **Peripherals (read-only)** list underneath Endpoints shows webcams, capture cards, and input devices that were detected but aren’t switchable computers. Those entries are informational only—you can’t click them and they won’t clutter the main endpoint list.

### Projecting Endpoints to Host Monitors

- Select an endpoint so its live video feed is running in the preview pane.
- In the **Host Displays** panel (bottom-right), click **Project Selected Endpoint** next to the monitor you want to occupy. The dashboard will open a full-screen window on that monitor and stretch the capture feed, effectively forcing that display to show the other PC even though it is only cabled to the host machine.
- Press `Esc` or double-click the projected screen to release it, or click **Release** in the Host Displays list.
- Use **Refresh Displays** if you plug in an additional monitor after launching the dashboard.

### Settings

- Open **File > Settings**
- Options available:
  - Start the dashboard minimized
  - Enable/disable the system tray icon
  - Require confirmation before exiting
  - Select a preferred theme placeholder (Dark/Light/System)
- Settings are persisted to `%LocalAppData%\KvmSwitch\settings.json`

### Viewing Settings

- Go to: **File > Settings** (currently shows placeholder)

### Updates & Patch Notes

- Open **Help > Check for Updates...** to compare your build with the published manifest at `docs/update-manifest.json`.
- If an update exists, click **Download Update** to open the release asset or **Open Release Notes** to read the changelog.
- Use **Help > Patch Notes / What's New** anytime to review the latest release notes even when you're already current.
- Fleet admins can run the companion console `KvmSwitch.Updater.exe --download` on target machines to fetch and launch the newest installer automatically.

## Important Notes

### The Dashboard is Standalone

**You do NOT need the HostService running** to use the Dashboard for:
- Viewing endpoints
- Configuring hotkeys
- Basic setup and testing

The HostService provides:
- Background hotkey handling
- Automatic device discovery
- Peripheral routing
- But the Dashboard can work without it for configuration

### Database Location

All configuration is stored in:
```
%LocalAppData%\KvmSwitch\endpoints.db
```
(Usually: `C:\Users\<YourUsername>\AppData\Local\KvmSwitch\endpoints.db`)

This database stores:
- Endpoint definitions
- Hotkey mappings
- Device information

## Troubleshooting

If the Dashboard won't launch or doesn't work:

1. **See the [Troubleshooting Guide](troubleshooting.md)** for detailed solutions
2. **Check Event Viewer** for error messages
3. **Run from command line** to see console output
4. **Verify .NET 8.0** is installed

## Next Steps

After launching the Dashboard:

1. **Verify the Host PC endpoint** appears in the list
2. **Connect a capture device** (HDMI or USB-C capture card)
3. **Refresh devices** to detect connected computers
4. **Configure hotkeys** for quick switching
5. **Test switching** between endpoints

## Getting Help

If you're still having issues:

1. Check the [Troubleshooting Guide](troubleshooting.md)
2. Review Event Viewer logs
3. Check console output when running from command line
4. Verify all prerequisites are installed (.NET 8.0 Runtime)

