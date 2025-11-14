# AuroraSwitch Installer Package

## Overview

The **AuroraSwitch KVM Suite** installer bundles the WPF dashboard (`KvmSwitch.Dashboard`) and the Windows host service (`KvmSwitch.HostService`) into a single executable built with Inno Setup. The package supports clean install/uninstall, registers the host service automatically, and ships with a build script for repeatable packaging.

## Prerequisites

- .NET SDK 8.0 or later (`dotnet --info` should succeed)
- Inno Setup 6 (`ISCC.exe`) installed in the default location (`C:\Program Files (x86)\Inno Setup 6\`)
- PowerShell 5.1+ (included with Windows 10/11)

## Build Pipeline

`installer/build-installer.ps1` orchestrates publishing and packaging:

1. Runs `dotnet publish` for `KvmSwitch.Dashboard` and `KvmSwitch.HostService` targeting `win-x64` into `installer/staging`.
2. Validates that the published executables exist.
3. Invokes `auroraswitch-setup.iss` via `ISCC.exe`, writing the installer to `installer/dist`.

### Usage

```powershell
cd "G:\KVM Software Switch\installer"
.\build-installer.ps1
```

Upon success the compiled installer (e.g., `AuroraSwitchSetup.exe`) resides in `installer/dist`.

## Installer Behavior

- **Installation Directory**: `%ProgramFiles%\AuroraSwitch`
  - `Dashboard\` contains `KvmSwitch.Dashboard.exe` and dependencies.
  - `HostService\` contains `KvmSwitch.HostService.exe` and dependencies.
- **Windows Service**: Registers `AuroraSwitchHostService` pointing to `HostService\KvmSwitch.HostService.exe` and starts it automatically.
- **Shortcuts**: Adds Start Menu and optional desktop shortcuts for the dashboard.
- **Uninstall**: Stops and removes the Windows service, then removes files and shortcuts.

## Customization

- Update product metadata (name, URLs) inside `installer/auroraswitch-setup.iss`.
- Replace the default icon by adding an `.ico` file and referencing it in the `[Setup]` section (`SetupIconFile=...`).
- To distribute additional artifacts (e.g., drivers), copy them into `installer/staging` before running the script and add corresponding `[Files]` entries.

## Verification

1. Install the generated `AuroraSwitchSetup.exe` on a test machine.
2. Confirm `AuroraSwitch Host Service` exists and is running (`services.msc` or `sc query`).
3. Launch the dashboard via the Start Menu shortcut.
4. Uninstall via Control Panel and verify the service and install directory are removed.

## Troubleshooting

- **ISCC.exe not found**: Install Inno Setup or update the path in `build-installer.ps1`.
- **Service fails to start**: Check Windows Event Viewer for `.NET` runtime errors; ensure the host service configuration files are present.
- **Missing dependencies**: Re-run the packaging script to regenerate the `staging` folder after any project changes.




