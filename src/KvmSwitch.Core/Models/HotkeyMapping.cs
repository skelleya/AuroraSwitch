namespace KvmSwitch.Core.Models;

/// <summary>
/// Maps a global hotkey combination to an endpoint ID.
/// </summary>
public class HotkeyMapping
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ModifierKeys Modifiers { get; set; }
    public int KeyCode { get; set; }
    public string EndpointId { get; set; } = string.Empty;
    public bool IsGlobal { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Flags]
public enum ModifierKeys
{
    None = 0,
    Ctrl = 1,
    Alt = 2,
    Shift = 4,
    Win = 8
}

