using KvmSwitch.Core.Models;

namespace KvmSwitch.Core.Interfaces;

/// <summary>
/// Discovers connected devices (USB-C, HDMI capture cards) and creates endpoint entries.
/// </summary>
public interface IDeviceDiscovery
{
    event EventHandler<Endpoint>? DeviceDiscovered;
    event EventHandler<string>? DeviceRemoved;
    
    Task StartDiscoveryAsync(CancellationToken cancellationToken = default);
    Task StopDiscoveryAsync();
    Task<IEnumerable<Endpoint>> ScanForDevicesAsync();
    Task<Endpoint?> DetectDeviceTypeAsync(string deviceId);
}

