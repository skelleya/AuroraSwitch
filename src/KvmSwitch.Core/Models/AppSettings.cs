namespace KvmSwitch.Core.Models;

/// <summary>
/// Represents user-configurable preferences for the dashboard shell.
/// </summary>
public class AppSettings
{
    public bool StartMinimized { get; set; } = false;
    public bool EnableSystemTray { get; set; } = true;
    public bool ConfirmOnExit { get; set; } = true;
    public string Theme { get; set; } = "Dark";

    public AppSettings Clone() => new()
    {
        StartMinimized = StartMinimized,
        EnableSystemTray = EnableSystemTray,
        ConfirmOnExit = ConfirmOnExit,
        Theme = Theme
    };
}


