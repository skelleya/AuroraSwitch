namespace KvmSwitch.Core.Models;

/// <summary>
/// Represents a target endpoint (secondary machine) that can receive peripheral input.
/// </summary>
public class Endpoint
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public EndpointType Type { get; set; }
    public ConnectionType ConnectionType { get; set; }
    public string? DeviceId { get; set; }
    public string? VendorId { get; set; }
    public string? ProductId { get; set; }
    public EndpointStatus Status { get; set; } = EndpointStatus.Disconnected;
    public DateTime LastSeen { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<string> CaptureDeviceIds { get; set; } = new();
}

public enum EndpointType
{
    Host,
    Windows,
    MacOS,
    Linux,
    Unknown
}

public enum ConnectionType
{
    UsbC,
    Hdmi,
    Network,
    Hybrid
}

public enum EndpointStatus
{
    Disconnected,
    Connecting,
    Connected,
    Active,
    Error
}

