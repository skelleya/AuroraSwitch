# Update Delivery Workflow

## Overview

AuroraSwitch now includes a lightweight update manifest and two delivery mechanisms:

1. **In-app checker** – `Help > Check for Updates...` downloads `docs/update-manifest.json`, compares versions, and links users to the latest installer / release notes.
2. **Headless updater** – `KvmSwitch.Updater.exe` can be shipped to IT admins to script deployments (`--manifest <url> --download`).

## Publishing a New Release

1. Bump the dashboard version in `src/KvmSwitch.Dashboard/KvmSwitch.Dashboard.csproj` (`<Version>`, `<FileVersion>`, `<AssemblyVersion>`). The in-app status bar and update checker both read this value, so the “tick” auto-increments every release.
2. Build a signed installer (`installer\build-installer.ps1`) and upload it to your GitHub release.
3. Edit `docs/update-manifest.json`:
   ```json
   {
     "version": "1.1.0",
     "downloadUrl": "https://github.com/your-org/AuroraSwitch/releases/download/v1.1.0/AuroraSwitchSetup.exe",
     "releaseNotesUrl": "https://github.com/your-org/AuroraSwitch/releases/tag/v1.1.0",
     "publishedAt": "2025-12-01T00:00:00Z",
     "notes": [
       "Short bullet describing change 1",
       "Short bullet describing change 2"
     ]
   }
   ```
4. Commit and push the manifest (or update it via GitHub web UI). By default clients fetch `https://raw.githubusercontent.com/skelleya/AuroraSwitch/master/docs/update-manifest.json`; override with the `AURORASWITCH_UPDATE_MANIFEST` environment variable if you host it elsewhere.

## Custom Manifest Location

Set the environment variable `AURORASWITCH_UPDATE_MANIFEST` on client machines (or via your deployment script) to point to a self-hosted manifest if needed.

## Silent Updates

Use the console updater in scripts:

```powershell
.\KvmSwitch.Updater.exe --manifest https://raw.githubusercontent.com/your-org/AuroraSwitch/main/docs/update-manifest.json --download
```

It will download the new installer to `%TEMP%` and launch it.


