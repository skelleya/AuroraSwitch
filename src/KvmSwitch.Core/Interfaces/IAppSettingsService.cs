using KvmSwitch.Core.Models;

namespace KvmSwitch.Core.Interfaces;

public interface IAppSettingsService
{
    event EventHandler<AppSettings>? SettingsChanged;

    AppSettings Current { get; }
    Task<AppSettings> GetAsync();
    Task SaveAsync(AppSettings settings);
}


