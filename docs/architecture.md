# Hybrid Software KVM Architecture

## Scope Confirmation
- The Windows host runs the primary KVM application built with `C#/WPF`.
- Peripheral redirection is implemented via USB-over-IP and optional USB-C direct tunneling.
- Video, audio, and EDID signals from secondary machines are ingested through capture hardware connected to the host.
- Global hotkeys on the host switch the active target so only one secondary machine receives HID input at a time.

## High-Level Components

### Host Control Service
- Orchestrates USB HID capture and forwarding, leveraging WinUSB or libusb for low-level access.
- Maintains exclusive focus on a single target at a time; all other endpoints are isolated.
- Exposes gRPC/Named Pipe API for UI and background services to coordinate state.
- Persists endpoint registry and hotkey mapping in a local SQLite database to ensure state survives restarts.
- Runs a watchdog thread that can force-switch focus back to the host when the user presses the emergency combo (`Ctrl+Shift+F12`).

### WPF Desktop UI
- Presents a tile-based dashboard showing live video feeds for each remote endpoint.
- Provides menu-driven assignment of hotkeys (e.g., `Ctrl+F1` host, `Ctrl+F2` HP laptop, `Ctrl+F3` MacBook Air).
- Displays connection health, bandwidth usage, and active peripheral routing state.
- Implements MVVM pattern with `Prism` for modularity; `DashboardViewModel` reacts to backend events via SignalR or gRPC streaming.
- Uses `D3DImage` interop to render Direct3D textures supplied by the capture pipeline, minimizing copy overhead.

### Video Capture Pipeline
- Utilizes DirectShow or Media Foundation to ingest HDMI/USB-C capture devices.
- Supports multiple concurrent capture devices for multi-monitor scenarios.
- Provides frame timing metadata to keep mouse movement latency under 30 ms when possible.
- Negotiates capture format (NV12 1080p60 baseline) and exposes texture handles for hardware-accelerated rendering.
- Implements optional scaling/compositing path to show picture-in-picture previews for inactive endpoints.

### Audio Routing
- Captures audio from the same devices as the video ingestion pipeline.
- Outputs audio through the host system default device or virtual audio cable for per-endpoint mixing.
- Provides optional echo cancellation when routing host microphone to a selected endpoint.
- Leverages WASAPI loopback capture and exclusive-mode output to minimize latency (<20 ms target).
- Supports per-endpoint gain control and mute state managed from the UI.

### Target Endpoint Adapters
- For USB-C targets (e.g., MacBook Air), detect via USB descriptors and negotiate alternate modes (DisplayPort Alt Mode, Thunderbolt).
- For HDMI-only targets, rely on capture card EDID and, when available, USB upstream for HID redirection.
- Include optional lightweight agent for secondary machines to expose management data (battery level, lid state) over LAN.
- Endpoint adapters encapsulate protocol differences (`UsbIpAdapter`, `NetworkAgentAdapter`) behind a shared `IEndpointSession` interface.
- Mac endpoints can optionally run a notarized helper app that exposes the `hidutil` relay service for better keyboard layout fidelity.

## Data Flow Summary
1. **Video/Audio Ingestion**: Capture devices push streams -> media pipeline -> WPF rendering layer.
2. **Peripheral Input**: Host intercepts HID -> routes via USB/IP stack -> selected endpoint.
3. **Hotkey Handling**: Global keyboard hook -> switch context -> update routing tables -> notify UI.
4. **Device Detection**: Hardware observer service polls USB and HDMI EDID descriptors -> updates endpoint registry.

### Subsystem Responsibilities
- **HID Capture Filter**: Kernel-mode driver that intercepts `HID_CLASS` traffic, publishes reports to shared memory, and respects allow/deny lists per device.
- **Transport Layer**: Encapsulates HID packets inside either TCP (USB-over-IP) or Thunderbolt control channels; negotiates encryption keys via the control service.
- **State Orchestrator**: Maintains authoritative record of active endpoint, pending handoffs, and peripheral reservations; persists to SQLite every 2 seconds.
- **Notification Hub**: Broadcasts toast notifications and UI badges whenever endpoints connect, disconnect, or lose sync; integrates with Windows Notification Platform.
- **Telemetry Collector**: Samples latency, packet loss, and frame pacing metrics; uploads anonymized diagnostics when the user opts in.

### Configuration & Persistence
- `AppData\\Roaming\\KvmSwitch\\settings.yaml` stores UI preferences (theme, layout density, audio mix levels).
- `%ProgramData%\\KvmSwitch\\endpoints.db` holds endpoint descriptors, EDID snapshots, and transport credentials.
- Secure secrets (e.g., remote agent tokens) are protected with DPAPI and optional user-provided passphrase.
- Configuration changes trigger hot reload via `FileSystemWatcher`, so admins can script deployments without restarting services.

## Key Requirements
- **Exclusive Control**: Only the active endpoint receives keyboard/mouse input; all others are locked out.
- **Hotkey Reliability**: Hotkeys must always return control to the host, even if the selected endpoint is unresponsive.
- **Low Latency**: Target round-trip latency under 40 ms for HID events.
- **Multi-Monitor**: Support at least two concurrent capture streams per endpoint with synchronized switching.
- **Cross-Platform**: Work with Windows, macOS, and Linux endpoints with minimal per-OS customization.
- **Resilience**: Host service must auto-recover and restore the previous active endpoint within 5 seconds of restart.
- **Security**: Mutual TLS for any remote agent communication; signed HID filter driver with secure boot compatibility.

## Interface Definitions
- `HostControl.proto`: gRPC contract exposing `ListEndpoints`, `ActivateEndpoint`, `AssignHotkey`, `GetRoutingStatus`, and `StreamDiagnostics`.
- `EndpointAgent.proto`: Agent callback contract covering `RequestPeripheralLock`, `ConfirmLock`, `Heartbeat`, and `TelemetrySnapshot`.
- `RoutingConfig.json`: Declarative mapping of physical capture inputs and USB device IDs to logical endpoints.
- `HotkeyConfig.json`: Stores global and per-endpoint hotkey assignments with scopes (`Global`, `UIOnly`) and override flags.

## Detection Strategy
- **USB-C**: Use Windows `SetupDi` APIs to identify connected USB Type-C devices, reading descriptors for platform hints (e.g., Apple Vendor ID).
- **HDMI**: Parse EDID blocks exposed by capture hardware to infer manufacturer/model; fall back to user-defined aliases when EDID is generic.
- **Network Agents**: Optional discovery using mDNS/Bonjour to augment hardware detection with hostnames and OS type.
- **Fallback**: Allow manual labeling when descriptors are insufficient, storing metadata in the endpoint registry.

## Deployment & Operations
- Installer (WiX Toolset) deploys Windows service (`KvmSwitch.HostService`), WPF UI (`KvmSwitch.Dashboard`), and signed HID filter driver.
- Service runs under `LocalSystem`; UI communicates via Named Pipes secured with per-user ACL tokens.
- Logging implemented via ETW provider `KvmSwitch.Host` with optional Serilog rolling files for field diagnostics.
- Health monitor watchdog ensures hotkey handler stays registered; on failure, it reinitializes hooks and warns the user via toast notifications.

## Security Considerations
- TLS transport for any network USB/IP channel.
- Signed drivers for USB filter operations.
- Audit logging for switching events and peripheral reassignments.

## Feasibility Assessment Highlights
- **USB HID redirection**: Use existing USB/IP stack (e.g., `usbip-win`) embedded as a service; requires driver signing but proven for HID bandwidths (<1 Mbps).
- **HDMI detection**: Dependent on capture card firmware exposing accurate EDID; testing shows typical consumer cards report pass-through manufacturer but not always model — mitigation via manual override.
- **MacBook multi-monitor**: Native macOS over USB-C supports dual displays only with DisplayPort MST hubs on Apple Silicon <macOS 14; DisplayLink docking station recommended for multi-monitor beyond one external display.
- **Latency**: Goal feasible with wired Gigabit LAN and hardware capture; wireless adds 10–20 ms jitter and may need QoS tuning.

## Next Steps
- Implement detailed interface definitions in `docs/wiki/backend.md`.
- Scaffold the WPF host solution (`src/HostApp`).
- Draft experimental plans for Mac multi-monitor support via DisplayLink.

