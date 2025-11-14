# Backend Architecture and Database Structure

## Overview

This document describes the backend architecture, database schema, and service interfaces for the KVM Software Switch system.

## Database Schema

### SQLite Database: `endpoints.db`

#### Table: `endpoints`

Stores discovered and configured endpoints (secondary machines).

| Column | Type | Description |
|--------|------|-------------|
| `id` | TEXT PRIMARY KEY | Unique endpoint identifier (GUID) |
| `name` | TEXT | User-friendly endpoint name |
| `type` | INTEGER | EndpointType enum (0=Host, 1=Windows, 2=MacOS, 3=Linux, 4=Unknown) |
| `connection_type` | INTEGER | ConnectionType enum (0=UsbC, 1=Hdmi, 2=Network, 3=Hybrid) |
| `device_id` | TEXT | Windows device ID or network address |
| `vendor_id` | TEXT | USB vendor ID (e.g., "05AC" for Apple) |
| `product_id` | TEXT | USB product ID |
| `status` | INTEGER | EndpointStatus enum (0=Disconnected, 1=Connecting, 2=Connected, 3=Active, 4=Error) |
| `last_seen` | TEXT | ISO 8601 timestamp of last detection |
| `metadata` | TEXT | JSON object with additional metadata |
| `created_at` | TEXT | ISO 8601 timestamp of creation |
| `updated_at` | TEXT | ISO 8601 timestamp of last update |

**Metadata JSON Structure**:
```json
{
  "capture_device_ids": ["device1", "device2"],
  "displaylink_detected": true,
  "os_version": "macOS 14.0",
  "hostname": "MacBook-Air.local"
}
```

#### Table: `hotkey_mappings`

Stores hotkey assignments for endpoint switching.

| Column | Type | Description |
|--------|------|-------------|
| `id` | TEXT PRIMARY KEY | Unique hotkey mapping identifier (GUID) |
| `modifiers` | INTEGER | ModifierKeys flags (Ctrl=1, Alt=2, Shift=4, Win=8) |
| `key_code` | INTEGER | Virtual key code (Windows VK_* constants) |
| `endpoint_id` | TEXT | Foreign key to endpoints.id |
| `is_global` | INTEGER | Boolean (1=true, 0=false) |
| `created_at` | TEXT | ISO 8601 timestamp |
| `updated_at` | TEXT | ISO 8601 timestamp |

**Indexes**:
- `idx_hotkey_combo` on (`modifiers`, `key_code`) - Ensures unique hotkey combinations
- `idx_endpoint_id` on (`endpoint_id`) - Fast endpoint lookup

#### Table: `routing_sessions`

Tracks active routing sessions for endpoints.

| Column | Type | Description |
|--------|------|-------------|
| `session_id` | TEXT PRIMARY KEY | Unique session identifier (GUID) |
| `endpoint_id` | TEXT | Foreign key to endpoints.id |
| `session_type` | TEXT | "UsbC" or "UsbIp" |
| `started_at` | TEXT | ISO 8601 timestamp |
| `last_activity` | TEXT | ISO 8601 timestamp of last input event |
| `is_active` | INTEGER | Boolean (1=active, 0=inactive) |

#### Table: `device_discovery_log`

Logs device discovery events for debugging.

### App Settings File (`settings.json`)

- Location: `%LocalAppData%\KvmSwitch\settings.json`
- Format: JSON serialized via `AppSettingsService`
- Fields:
  - `startMinimized` (bool) — whether to hide to tray on startup
  - `enableSystemTray` (bool) — controls NotifyIcon creation
  - `confirmOnExit` (bool) — prompts the user before closing
  - `theme` (string) — placeholder for UI theming
- The Dashboard loads settings during startup and exposes them through **File > Settings**

| Column | Type | Description |
|--------|------|-------------|
| `id` | INTEGER PRIMARY KEY AUTOINCREMENT | Auto-incrementing ID |
| `device_id` | TEXT | Windows device ID |
| `event_type` | TEXT | "Discovered", "Removed", "Updated" |
| `endpoint_id` | TEXT | Associated endpoint ID (nullable) |
| `timestamp` | TEXT | ISO 8601 timestamp |
| `details` | TEXT | JSON object with event details |

## Service Interfaces

### IEndpointRegistry

Manages endpoint registration and persistence.

**Methods**:
- `Task<IEnumerable<Endpoint>> GetAllEndpointsAsync()`
- `Task<Endpoint?> GetEndpointByIdAsync(string id)`
- `Task<Endpoint?> GetEndpointByHotkeyAsync(ModifierKeys modifiers, int keyCode)`
- `Task AddOrUpdateEndpointAsync(Endpoint endpoint)`
- `Task RemoveEndpointAsync(string id)`
- `Task SaveAsync()`
- `Task LoadAsync()`

**Events**:
- `event EventHandler<Endpoint>? EndpointAdded`
- `event EventHandler<Endpoint>? EndpointRemoved`
- `event EventHandler<Endpoint>? EndpointUpdated`

### IHotkeyManager

Manages global hotkey registration and handling.

**Methods**:
- `Task<bool> RegisterHotkeyAsync(HotkeyMapping mapping)`
- `Task<bool> UnregisterHotkeyAsync(string hotkeyId)`
- `Task<IEnumerable<HotkeyMapping>> GetRegisteredHotkeysAsync()`
- `Task<bool> IsHotkeyAvailableAsync(ModifierKeys modifiers, int keyCode)`
- `void StartListening()`
- `void StopListening()`

**Events**:
- `event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed`

### IDeviceDiscovery

Discovers connected USB-C and HDMI capture devices.

**Methods**:
- `Task StartDiscoveryAsync(CancellationToken cancellationToken = default)`
- `Task StopDiscoveryAsync()`
- `Task<IEnumerable<Endpoint>> ScanForDevicesAsync()`
- `Task<Endpoint?> DetectDeviceTypeAsync(string deviceId)`

**Events**:
- `event EventHandler<Endpoint>? DeviceDiscovered`
- `event EventHandler<string>? DeviceRemoved`

### IPeripheralRouter

Routes keyboard and mouse input to active endpoint.

**Methods**:
- `Task<bool> SwitchToEndpointAsync(string endpointId)`
- `Task<bool> SwitchToHostAsync()`
- `Task<bool> SendKeyboardInputAsync(string endpointId, byte[] keyData)`
- `Task<bool> SendMouseInputAsync(string endpointId, int deltaX, int deltaY, MouseButtonState buttons)`
- `Task LockAllEndpointsAsync()`
- `Task UnlockEndpointAsync(string endpointId)`

**Properties**:
- `string? ActiveEndpointId { get; }`

**Events**:
- `event EventHandler<string>? ActiveEndpointChanged`

### IEndpointSession

Represents an active session with an endpoint.

**Methods**:
- `Task<bool> ConnectAsync(CancellationToken cancellationToken = default)`
- `Task DisconnectAsync()`
- `Task<bool> SendKeyboardInputAsync(byte[] keyData)`
- `Task<bool> SendMouseInputAsync(int deltaX, int deltaY, MouseButtonState buttons)`
- `Task<bool> LockPeripheralsAsync()`
- `Task<bool> UnlockPeripheralsAsync()`

**Properties**:
- `string EndpointId { get; }`
- `EndpointStatus Status { get; }`
- `bool IsActive { get; }`

## gRPC Service Definitions

### HostControl Service

**Proto File**: `HostControl.proto`

```protobuf
service HostControl {
  rpc ListEndpoints(ListEndpointsRequest) returns (ListEndpointsResponse);
  rpc ActivateEndpoint(ActivateEndpointRequest) returns (ActivateEndpointResponse);
  rpc AssignHotkey(AssignHotkeyRequest) returns (AssignHotkeyResponse);
  rpc GetRoutingStatus(GetRoutingStatusRequest) returns (GetRoutingStatusResponse);
  rpc StreamDiagnostics(StreamDiagnosticsRequest) returns (stream DiagnosticsSnapshot);
}

message ListEndpointsRequest {}

message ListEndpointsResponse {
  repeated Endpoint endpoints = 1;
}

message ActivateEndpointRequest {
  string endpoint_id = 1;
}

message ActivateEndpointResponse {
  bool success = 1;
  string error_message = 2;
}

message AssignHotkeyRequest {
  string endpoint_id = 1;
  int32 modifiers = 2;
  int32 key_code = 3;
}

message AssignHotkeyResponse {
  bool success = 1;
  string error_message = 2;
}

message GetRoutingStatusRequest {}

message GetRoutingStatusResponse {
  string active_endpoint_id = 1;
  repeated EndpointStatus endpoints = 2;
}

message StreamDiagnosticsRequest {}

message DiagnosticsSnapshot {
  int64 timestamp = 1;
  string active_endpoint_id = 2;
  repeated EndpointMetrics endpoints = 3;
  SystemMetrics system = 4;
}
```

## Configuration Files

### routing.json

Stores routing configuration and device mappings.

```json
{
  "default_endpoint": "host",
  "auto_switch_on_disconnect": true,
  "emergency_hotkey": {
    "modifiers": 7,
    "key_code": 123
  },
  "device_mappings": [
    {
      "capture_device_id": "device1",
      "endpoint_id": "endpoint1",
      "display_index": 0
    }
  ]
}
```

### hotkeys.json

Stores hotkey configuration (backup/override for database).

```json
{
  "hotkeys": [
    {
      "id": "hotkey1",
      "modifiers": 1,
      "key_code": 112,
      "endpoint_id": "host"
    },
    {
      "id": "hotkey2",
      "modifiers": 1,
      "key_code": 113,
      "endpoint_id": "endpoint1"
    }
  ]
}
```

## Data Flow

### Endpoint Discovery Flow

1. `DeviceDiscovery` polls USB devices via WMI
2. Detects USB-C devices and reads descriptors
3. Identifies vendor/product IDs
4. Creates `Endpoint` object
5. Calls `IEndpointRegistry.AddOrUpdateEndpointAsync()`
6. Registry persists to SQLite database
7. Registry fires `EndpointAdded` event
8. UI updates endpoint list

### Hotkey Switching Flow

1. User presses hotkey (e.g., Ctrl+F2)
2. `HotkeyManager` receives low-level keyboard hook event
3. Fires `HotkeyPressed` event with endpoint ID
4. `PeripheralRouter` receives event
5. Calls `SwitchToEndpointAsync(endpointId)`
6. Locks all endpoints
7. Creates/retrieves `IEndpointSession` for endpoint
8. Calls `session.ConnectAsync()`
9. Calls `session.LockPeripheralsAsync()`
10. Updates `ActiveEndpointId`
11. Fires `ActiveEndpointChanged` event
12. UI updates status display

### Peripheral Input Flow

1. User moves mouse or types on keyboard
2. Windows HID driver captures input
3. `PeripheralRouter` intercepts HID events
4. Routes to active endpoint via `IEndpointSession`
5. Session converts to USB HID report format
6. Sends via USB-C (direct) or USB/IP (network)
7. Target machine receives HID report
8. Target OS processes input

## Security Considerations

### Database Security

- SQLite database stored in `%LocalAppData%\KvmSwitch\`
- Protected by Windows file system permissions
- No network exposure

### gRPC Security

- TLS encryption for network communication
- Mutual authentication for agent connections
- Named pipes for local communication (Windows security)

### USB Device Access

- Requires administrator privileges
- Uses Windows Device Manager APIs
- No unsigned driver installation required

## Performance Considerations

### Database Queries

- Endpoints cached in memory (`ConcurrentDictionary`)
- Database writes batched (not per-operation)
- Indexes on hotkey combinations for fast lookup

### Session Management

- Sessions pooled and reused
- Idle sessions closed after 5 minutes
- Connection retry with exponential backoff

### Input Latency

- HID events processed synchronously (no queuing)
- USB-C: Direct hardware access (< 5ms overhead)
- USB/IP: Network latency dependent (< 20ms on Gigabit LAN)
