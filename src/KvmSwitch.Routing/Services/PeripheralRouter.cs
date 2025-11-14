using KvmSwitch.Core.Interfaces;
using KvmSwitch.Core.Models;
using KvmSwitch.Routing.Interfaces;
using KvmSwitch.Routing.Sessions;
using Microsoft.Extensions.Logging;

namespace KvmSwitch.Routing.Services;

public class PeripheralRouter : IPeripheralRouter
{
    private readonly IEndpointRegistry _endpointRegistry;
    private readonly ILogger<PeripheralRouter>? _logger;
    private string? _activeEndpointId;
    private readonly Dictionary<string, IEndpointSession> _sessions = new();

    public string? ActiveEndpointId => _activeEndpointId;

    public event EventHandler<string>? ActiveEndpointChanged;

    public PeripheralRouter(IEndpointRegistry endpointRegistry, ILogger<PeripheralRouter>? logger = null)
    {
        _endpointRegistry = endpointRegistry;
        _logger = logger;
    }

    public async Task<bool> SwitchToEndpointAsync(string endpointId)
    {
        try
        {
            // Lock all endpoints first
            await LockAllEndpointsAsync();

            // Get endpoint from registry
            var endpoint = await _endpointRegistry.GetEndpointByIdAsync(endpointId);
            if (endpoint == null)
            {
                _logger?.LogWarning("Endpoint not found: {EndpointId}", endpointId);
                return false;
            }

            // For HDMI-only endpoints, we don't need USB connection - just mark as active
            // Video capture will handle the HDMI connection separately
            if (endpoint.ConnectionType == ConnectionType.Hdmi)
            {
                _logger?.LogInformation("Switching to HDMI-only endpoint: {EndpointId} ({EndpointName}) - USB connection not required", endpointId, endpoint.Name);
                _activeEndpointId = endpointId;
                ActiveEndpointChanged?.Invoke(this, endpointId);
                return true;
            }

            // For USB-based endpoints, create or get session
            if (!_sessions.TryGetValue(endpointId, out var session))
            {
                session = EndpointSessionFactory.CreateSession(endpoint);
                _sessions[endpointId] = session;
            }

            // Connect with timeout (5 seconds)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var connected = await session.ConnectAsync(cts.Token);
            
            if (!connected)
            {
                // For USB-C endpoints that aren't implemented yet, allow switching anyway if it's HDMI-capable
                if (endpoint.ConnectionType == ConnectionType.Hybrid || endpoint.ConnectionType == ConnectionType.UsbC)
                {
                    _logger?.LogWarning("USB connection failed for endpoint: {EndpointId}, but continuing for HDMI video support", endpointId);
                    // Still allow the switch - video capture may still work
                }
                else
                {
                    _logger?.LogError("Failed to connect to endpoint: {EndpointId}", endpointId);
                    return false;
                }
            }

            await session.LockPeripheralsAsync();
            _activeEndpointId = endpointId;
            ActiveEndpointChanged?.Invoke(this, endpointId);
            
            _logger?.LogInformation("Switched to endpoint: {EndpointId} ({EndpointName})", endpointId, endpoint.Name);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Connection timeout switching to endpoint: {EndpointId}", endpointId);
            // For HDMI endpoints, still allow the switch
            var endpoint = await _endpointRegistry.GetEndpointByIdAsync(endpointId);
            if (endpoint != null && (endpoint.ConnectionType == ConnectionType.Hdmi || endpoint.ConnectionType == ConnectionType.Hybrid))
            {
                _activeEndpointId = endpointId;
                ActiveEndpointChanged?.Invoke(this, endpointId);
                _logger?.LogInformation("Switched to endpoint (HDMI mode): {EndpointId} despite USB connection timeout", endpointId);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error switching to endpoint: {EndpointId}", endpointId);
            return false;
        }
    }

    public async Task<bool> SwitchToHostAsync()
    {
        if (_activeEndpointId == null)
        {
            return true; // Already on host
        }

        try
        {
            // Unlock current endpoint
            if (_sessions.TryGetValue(_activeEndpointId, out var session))
            {
                await session.UnlockPeripheralsAsync();
                await session.DisconnectAsync();
            }

            _activeEndpointId = null;
            ActiveEndpointChanged?.Invoke(this, "host");
            
            _logger?.LogInformation("Switched back to host");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error switching to host");
            return false;
        }
    }

    public async Task<bool> SendKeyboardInputAsync(string endpointId, byte[] keyData)
    {
        if (!_sessions.TryGetValue(endpointId, out var session))
        {
            return false;
        }

        // Convert to HID report format if needed
        byte[] hidReport;
        if (keyData.Length == 8)
        {
            hidReport = keyData; // Already in HID format
        }
        else
        {
            // Assume it's a VK code, convert to HID report
            var vkCode = keyData.Length > 0 ? keyData[0] : 0;
            hidReport = Services.HidReportConverter.CreateKeyboardReport(vkCode, ModifierKeys.None);
        }

        return await session.SendKeyboardInputAsync(hidReport);
    }

    public async Task<bool> SendMouseInputAsync(string endpointId, int deltaX, int deltaY, MouseButtonState buttons)
    {
        if (!_sessions.TryGetValue(endpointId, out var session))
        {
            return false;
        }

        return await session.SendMouseInputAsync(deltaX, deltaY, buttons);
    }

    public async Task LockAllEndpointsAsync()
    {
        foreach (var session in _sessions.Values)
        {
            try
            {
                await session.LockPeripheralsAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error locking endpoint session");
            }
        }
    }

    public async Task UnlockEndpointAsync(string endpointId)
    {
        if (_sessions.TryGetValue(endpointId, out var session))
        {
            await session.UnlockPeripheralsAsync();
        }
    }
}

