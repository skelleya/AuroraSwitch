using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using KvmSwitch.Core.Interfaces;
using KvmSwitch.Core.Models;
using Microsoft.Extensions.Logging;

namespace KvmSwitch.Core.Services;

public class DeviceDiscovery : IDeviceDiscovery
{
    private readonly ILogger<DeviceDiscovery>? _logger;
    private static readonly Dictionary<string, EndpointType> VendorEndpointMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["05AC"] = EndpointType.MacOS,    // Apple
        ["03F0"] = EndpointType.Windows,  // HP
        ["17EF"] = EndpointType.Windows,  // Lenovo
        ["04CA"] = EndpointType.Windows,  // Lite-On (common in laptops)
        ["045E"] = EndpointType.Windows   // Microsoft Surface
    };
    private static readonly string[] AppleKeywords =
    {
        "apple",
        "macbook",
        "mac book",
        "macbook air",
        "macbook pro",
        "mac mini",
        "macos"
    };
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _discoveryTask;

    public event EventHandler<Endpoint>? DeviceDiscovered;
    public event EventHandler<string>? DeviceRemoved;

    public DeviceDiscovery(ILogger<DeviceDiscovery>? logger = null)
    {
        _logger = logger;
    }

    public Task StartDiscoveryAsync(CancellationToken cancellationToken = default)
    {
        if (_discoveryTask != null && !_discoveryTask.IsCompleted)
        {
            return Task.CompletedTask;
        }

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _discoveryTask = Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await ScanForDevicesAsync();
                    await Task.Delay(5000, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during device discovery");
                }
            }
        }, _cancellationTokenSource.Token);

        _logger?.LogInformation("Device discovery started");
        return Task.CompletedTask;
    }

    public Task StopDiscoveryAsync()
    {
        _cancellationTokenSource?.Cancel();
        _discoveryTask?.Wait(TimeSpan.FromSeconds(5));
        _logger?.LogInformation("Device discovery stopped");
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Endpoint>> ScanForDevicesAsync()
    {
        var endpoints = new List<Endpoint>();

        // Scan USB devices via PnP entities to catch USB-C endpoints connected through adapters
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT DeviceID, Name, Manufacturer, PNPClass FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%VID_%'");
            foreach (ManagementObject device in searcher.Get())
            {
                var endpoint = CreateUsbEndpoint(device);
                if (endpoint != null)
                {
                    endpoints.Add(endpoint);
                    DeviceDiscovered?.Invoke(this, endpoint);
                    _logger?.LogInformation("Detected USB endpoint {Name} ({Vendor}:{Product})", endpoint.Name, endpoint.VendorId, endpoint.ProductId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error scanning USB devices");
        }

        // Scan display devices so HDMI sources such as Apple laptops are detectable
        try
        {
            using var displaySearcher = new ManagementObjectSearcher("SELECT DeviceID, Name, Manufacturer, PNPClass FROM Win32_PnPEntity WHERE PNPClass = 'DISPLAY'");
            foreach (ManagementObject display in displaySearcher.Get())
            {
                var endpoint = CreateDisplayEndpoint(display);
                if (endpoint == null)
                {
                    continue;
                }

                endpoints.Add(endpoint);
                DeviceDiscovered?.Invoke(this, endpoint);
                _logger?.LogInformation("Detected display endpoint {Name} ({DeviceId})", endpoint.Name, endpoint.DeviceId);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error scanning display devices");
        }

        // Scan for HDMI capture devices using DirectShow
        try
        {
            var captureDevices = KvmSwitch.Capture.Services.CaptureDeviceEnumerator.GetCaptureDevices();
            foreach (var captureDevice in captureDevices)
            {
                var endpoint = new Endpoint
                {
                    Id = $"capture:{SanitizeDeviceId(captureDevice.DevicePath)}",
                    Name = captureDevice.Name,
                    DeviceId = captureDevice.DevicePath,
                    ConnectionType = ConnectionType.Hdmi,
                    Type = DetectCaptureDeviceType(captureDevice.Name),
                    Status = EndpointStatus.Disconnected,
                    LastSeen = DateTime.UtcNow
                };

                endpoint.CaptureDeviceIds.Add(captureDevice.DevicePath);
                endpoints.Add(endpoint);
                DeviceDiscovered?.Invoke(this, endpoint);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error scanning capture devices");
        }

        return Task.FromResult<IEnumerable<Endpoint>>(endpoints);
    }

    public Task<Endpoint?> DetectDeviceTypeAsync(string deviceId)
    {
        var endpoint = CreateEndpointFromDeviceId(deviceId);
        return Task.FromResult(endpoint);
    }

    private EndpointType DetectCaptureDeviceType(string deviceName)
    {
        // Try to detect endpoint type from capture device name
        var nameLower = deviceName.ToLowerInvariant();
        
        // This is a capture device, so we can't directly detect the connected machine
        // User will need to configure this manually or we'll use a default
        return EndpointType.Unknown;
    }

    private Endpoint? CreateUsbEndpoint(ManagementObject device)
    {
        var deviceId = device["DeviceID"]?.ToString();
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return null;
        }

        var manufacturer = device["Manufacturer"]?.ToString();
        var friendlyName = device["Name"]?.ToString();

        var endpoint = CreateEndpointFromDeviceId(deviceId);
        if (endpoint == null && LooksLikeAppleDevice(friendlyName, manufacturer, deviceId))
        {
            endpoint = CreateEndpointFromDeviceId(
                deviceId,
                EndpointType.MacOS,
                ConnectionType.UsbC,
                friendlyName ?? "MacBook (USB-C Adapter)");
        }

        if (endpoint == null)
        {
            return null;
        }

        endpoint.Name = friendlyName ?? endpoint.Name;
        if (!string.IsNullOrWhiteSpace(manufacturer))
        {
            endpoint.Metadata["manufacturer"] = manufacturer;
        }

        if (string.IsNullOrWhiteSpace(endpoint.Name))
        {
            endpoint.Name = manufacturer ?? "USB Device";
        }

        return endpoint;
    }

    private Endpoint? CreateEndpointFromDeviceId(
        string deviceId,
        EndpointType? forcedType = null,
        ConnectionType? forcedConnectionType = null,
        string? defaultName = null)
    {
        var vendorId = ExtractToken(deviceId, "VID_");
        var productId = ExtractToken(deviceId, "PID_");
        var sanitizedId = SanitizeDeviceId(deviceId);

        var vendorRecognized = vendorId != null && VendorEndpointMap.ContainsKey(vendorId);
        if (!vendorRecognized && !forcedType.HasValue)
        {
            _logger?.LogDebug("Skipping USB device {DeviceId} (vendor {VendorId}) - not recognized as endpoint", deviceId, vendorId);
            return null;
        }

        var endpoint = new Endpoint
        {
            Id = $"usb:{sanitizedId}",
            DeviceId = deviceId,
            VendorId = vendorId,
            ProductId = productId,
            ConnectionType = forcedConnectionType ?? ConnectionType.UsbC,
            Status = EndpointStatus.Connected,
            LastSeen = DateTime.UtcNow
        };

        endpoint.Type = forcedType ?? InferEndpointType(vendorId, productId);
        endpoint.Name = defaultName ?? endpoint.Type switch
        {
            EndpointType.MacOS => "MacBook (USB-C)",
            EndpointType.Windows => "Windows Laptop (USB-C)",
            EndpointType.Linux => "Linux Device (USB-C)",
            _ => "USB Endpoint"
        };

        return endpoint;
    }

    private static string? ExtractToken(string deviceId, string token)
    {
        var index = deviceId.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if (index < 0 || index + token.Length + 4 > deviceId.Length)
        {
            return null;
        }
        return deviceId.Substring(index + token.Length, 4).ToUpperInvariant();
    }

    private static string SanitizeDeviceId(string deviceId)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(deviceId.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return sanitized.Replace("\\", "_").Replace("/", "_").Replace("#", "_").ToLowerInvariant();
    }

    private static EndpointType InferEndpointType(string? vendorId, string? productId)
    {
        if (vendorId != null && VendorEndpointMap.TryGetValue(vendorId, out var endpointType))
        {
            return endpointType;
        }

        return EndpointType.Unknown;
    }

    private Endpoint? CreateDisplayEndpoint(ManagementObject device)
    {
        var deviceId = device["DeviceID"]?.ToString();
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return null;
        }

        var name = device["Name"]?.ToString();
        var manufacturer = device["Manufacturer"]?.ToString();
        if (!LooksLikeAppleDevice(name, manufacturer, deviceId))
        {
            return null;
        }

        var endpoint = new Endpoint
        {
            Id = $"display:{SanitizeDeviceId(deviceId)}",
            DeviceId = deviceId,
            Name = string.IsNullOrWhiteSpace(name) ? "Apple HDMI Source" : name,
            ConnectionType = ConnectionType.Hdmi,
            Type = EndpointType.MacOS,
            Status = EndpointStatus.Connected,
            LastSeen = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(manufacturer))
        {
            endpoint.Metadata["manufacturer"] = manufacturer;
        }

        endpoint.Metadata["pnp_class"] = device["PNPClass"]?.ToString() ?? "DISPLAY";
        return endpoint;
    }

    private static bool LooksLikeAppleDevice(string? name, string? manufacturer, string? deviceId)
    {
        if (!string.IsNullOrWhiteSpace(manufacturer) &&
            manufacturer.Contains("Apple", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(name) &&
            AppleKeywords.Any(keyword => name.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            if (deviceId.StartsWith("DISPLAY\\APP", StringComparison.OrdinalIgnoreCase) ||
                deviceId.Contains("VID_05AC", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (AppleKeywords.Any(keyword =>
                    deviceId.Contains(keyword.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }
}

