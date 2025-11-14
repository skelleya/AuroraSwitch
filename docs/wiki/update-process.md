# Update Delivery Workflow

## Overview

AuroraSwitch now includes a lightweight update manifest and two delivery mechanisms:

1. **In-app checker** – `Help > Check for Updates...` downloads `docs/update-manifest.json`, compares versions, and links users to the latest installer / release notes.
2. **Headless updater** – `KvmSwitch.Updater.exe` can be shipped to IT admins to script deployments (`--manifest <url> --download`).

## Publishing a New Release

1. Bump the dashboard version in `src/KvmSwitch.Dashboard/KvmSwitch.Dashboard.csproj` (`<Version>`, `<FileVersion>`, `<AssemblyVersion>`). The in-app status bar and update checker both read this value, so the “tick” auto-increments every release.
2. Build a signed installer (`installer\build-installer.ps1`) and upload it as an asset on a new GitHub Release (use a tag such as `v1.3.0` that matches the version you just baked into the csproj). Add release notes in the body—those lines are automatically parsed into the in-app “Release notes” panel.
3. Done! The dashboard now queries `https://api.github.com/repos/skelleya/AuroraSwitch/releases/latest` by default, so as soon as the release is published, “Check for Updates…” sees it and the in-app downloader will pull the `.exe` asset directly from GitHub.
4. Optional: if you still need to point at a custom manifest or self-hosted installer, set `AURORASWITCH_UPDATE_MANIFEST` to a JSON URL and the legacy manifest path will override the GitHub release flow.

## Custom Manifest Location

Set the environment variable `AURORASWITCH_UPDATE_MANIFEST` on client machines (or via your deployment script) to point to a self-hosted manifest if needed.

## Silent Updates

Use the console updater in scripts:

```powershell
.\KvmSwitch.Updater.exe --manifest https://raw.githubusercontent.com/your-org/AuroraSwitch/main/docs/update-manifest.json --download
```

It will download the new installer to `%TEMP%` and launch it.


