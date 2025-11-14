# KVM Software Switch

A custom software KVM solution that allows you to control multiple computers (including HP Laptop and MacBook Air M2) from a single Windows host PC using hotkeys. The software routes keyboard and mouse input to the selected machine while displaying video feeds from capture devices.

## Features

- **Hotkey-Based Switching**: Use Ctrl+F1 for host, Ctrl+F2+ for secondary machines
- **USB-C and USB/IP Support**: Direct USB-C connection or network-based USB/IP
- **Video Capture Integration**: Display video feeds from HDMI capture devices
- **Exclusive Control**: Only the active endpoint receives input; all others are locked out
- **Device Auto-Detection**: Automatically detects USB-C devices and identifies MacBook Air/HP Laptop
- **Multi-Monitor Support**: DisplayLink support for MacBook Air M2 multi-monitor setups
- **Low Latency**: Target < 100ms switching latency, < 20ms input latency for USB-C

## Quick Start

1. **Prerequisites**
   - Windows 10/11 (64-bit)
   - Administrator privileges
   - .NET 8.0 Runtime
   - USB-C or HDMI capture hardware

2. **Installation**
   - Run the installer as Administrator
   - Launch "KVM Software Switch" from Start Menu

3. **First Use**
   - Connect your secondary machines (HP Laptop, MacBook Air)
   - Click "Refresh Devices" in the menu
   - Assign hotkeys to endpoints
   - Press Ctrl+F2 to switch to first secondary machine

## Documentation

- **[Architecture Overview](docs/architecture.md)**: System architecture and component design
- **[Setup Guide](docs/wiki/README.md)**: Detailed setup and configuration instructions
- **[Hardware Requirements](docs/wiki/README.md#hardware-requirements)**: Required hardware and recommendations
- **[Testing Procedures](docs/wiki/testing.md)**: Test matrix and validation procedures
- **[Backend Documentation](docs/wiki/backend.md)**: Database schema and service interfaces
- **[Mac Multi-Monitor Guide](docs/wiki/mac-multimonitor.md)**: DisplayLink setup for MacBook Air

## Project Structure

```
.
├── src/                          # Source code
│   ├── KvmSwitch.Core/          # Core interfaces and models
│   ├── KvmSwitch.Dashboard/     # WPF desktop application
│   ├── KvmSwitch.HostService/   # Windows service
│   ├── KvmSwitch.Capture/       # Video/audio capture
│   └── KvmSwitch.Routing/       # Peripheral routing
├── docs/                         # Documentation
│   ├── architecture.md          # Architecture overview
│   └── wiki/                    # Comprehensive documentation
│       ├── README.md            # Main documentation
│       ├── backend.md           # Backend architecture
│       ├── testing.md           # Testing procedures
│       └── mac-multimonitor.md  # Mac multi-monitor guide
└── README.md                    # This file
```

## Building from Source

**Requirements**:
- Visual Studio 2022 or .NET 8.0 SDK
- Windows SDK

**Build**:
```bash
cd src
dotnet restore
dotnet build
```

## Key Technologies

- **C# / .NET 8.0**: Primary development language and runtime
- **WPF**: Desktop UI framework
- **DirectShow / Media Foundation**: Video capture
- **USB/IP**: Network-based USB device sharing
- **LibUsbDotNet**: USB-C device access
- **SQLite**: Endpoint and configuration storage
- **gRPC**: Service communication (planned)

## Known Limitations

1. **MacBook Air Multi-Monitor**: Requires DisplayLink-compatible dock (macOS limitation)
2. **DisplayLink Latency**: Adds 20-50ms latency compared to native displays
3. **USB/IP Network Dependency**: Requires stable Gigabit LAN for acceptable latency
4. **Administrator Privileges**: Required for USB device access and global hotkeys

## Contributing

Contributions are welcome! Please see the documentation in `docs/` for architecture details and coding guidelines.

## License

[Specify license here]

## Support

For issues or questions:
- Check [Troubleshooting Guide](docs/wiki/README.md#troubleshooting)
- Review [Known Issues](docs/wiki/README.md#known-issues)
- Open an issue on GitHub

