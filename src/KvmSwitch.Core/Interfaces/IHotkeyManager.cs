using KvmSwitch.Core.Models;

namespace KvmSwitch.Core.Interfaces;

/// <summary>
/// Manages global hotkey registration and handling.
/// </summary>
public interface IHotkeyManager
{
    event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;
    
    Task<bool> RegisterHotkeyAsync(HotkeyMapping mapping);
    Task<bool> UnregisterHotkeyAsync(string hotkeyId);
    Task<IEnumerable<HotkeyMapping>> GetRegisteredHotkeysAsync();
    Task<bool> IsHotkeyAvailableAsync(ModifierKeys modifiers, int keyCode);
    void StartListening();
    void StopListening();
}

public class HotkeyPressedEventArgs : EventArgs
{
    public string EndpointId { get; set; } = string.Empty;
    public ModifierKeys Modifiers { get; set; }
    public int KeyCode { get; set; }
}

