## Scope Confirmation — Hybrid Software KVM

### Decision Summary
- **Architecture**: Proceed with a hybrid software KVM where the Windows host manages USB-over-IP peripheral redirection while ingesting HDMI/USB-C video/audio via capture hardware.
- **Host Platform**: Windows 11 (primary development) with support for additional Windows hosts in future iterations.
- **Client Targets**: HP Windows laptop and MacBook Air (Apple Silicon) as primary validation platforms; extensible to other USB-C/HDMI-capable systems.
- **Input Exclusivity**: Only the currently selected endpoint receives HID events; all other endpoints (including the host) remain isolated until re-selected.
- **Hotkey Scheme**: Global hotkeys (e.g., `Ctrl+F1` for host, `Ctrl+F2` for HP laptop, `Ctrl+F3` for MacBook Air) reserved for switching focus; configurable via UI.

### Rationale
- Combines low-latency hardware capture with flexible software-based peripheral routing, avoiding the need for full hardware KVM switches.
- Leverages existing USB/IP tooling and Windows APIs for hotkeys and device management.
- Provides a path to multi-monitor and cross-platform support with manageable complexity.

### Open Items
- Validate whether USB-C alternate mode negotiation is sufficient for full-featured MacBook Air support or if Thunderbolt/DisplayLink adapters are required.
- Confirm capture hardware capabilities for dual-monitor ingestion prior to procurement.

