using KvmSwitch.Core.Interfaces;

namespace KvmSwitch.Routing.Interfaces;

/// <summary>
/// Routes keyboard and mouse input to the active endpoint.
/// </summary>
public interface IPeripheralRouter
{
    string? ActiveEndpointId { get; }
    
    event EventHandler<string>? ActiveEndpointChanged;
    
    Task<bool> SwitchToEndpointAsync(string endpointId);
    Task<bool> SwitchToHostAsync();
    Task<bool> SendKeyboardInputAsync(string endpointId, byte[] keyData);
    Task<bool> SendMouseInputAsync(string endpointId, int deltaX, int deltaY, MouseButtonState buttons);
    Task LockAllEndpointsAsync();
    Task UnlockEndpointAsync(string endpointId);
}

