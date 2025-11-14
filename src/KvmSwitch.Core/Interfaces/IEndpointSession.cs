using KvmSwitch.Core.Models;

namespace KvmSwitch.Core.Interfaces;

/// <summary>
/// Represents an active session with an endpoint for peripheral routing.
/// </summary>
public interface IEndpointSession : IDisposable
{
    string EndpointId { get; }
    EndpointStatus Status { get; }
    bool IsActive { get; }
    
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task<bool> SendKeyboardInputAsync(byte[] keyData);
    Task<bool> SendMouseInputAsync(int deltaX, int deltaY, MouseButtonState buttons);
    Task<bool> LockPeripheralsAsync();
    Task<bool> UnlockPeripheralsAsync();
}

public struct MouseButtonState
{
    public bool LeftButton { get; set; }
    public bool RightButton { get; set; }
    public bool MiddleButton { get; set; }
    public int ScrollDelta { get; set; }
}

