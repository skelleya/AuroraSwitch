using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using KvmSwitch.Core.Interfaces;
using KvmSwitch.Core.Models;
using Microsoft.Extensions.Logging;

namespace KvmSwitch.Core.Services;

public class AppSettingsService : IAppSettingsService
{
    private readonly string _settingsPath;
    private readonly ILogger<AppSettingsService>? _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AppSettings _current = new();
    private bool _isLoaded;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public event EventHandler<AppSettings>? SettingsChanged;

    public AppSettings Current => _current.Clone();

    public AppSettingsService(ILogger<AppSettingsService>? logger = null)
    {
        _logger = logger;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var basePath = Path.Combine(appData, "KvmSwitch");
        Directory.CreateDirectory(basePath);
        _settingsPath = Path.Combine(basePath, "settings.json");
    }

    public async Task<AppSettings> GetAsync()
    {
        await EnsureLoadedAsync().ConfigureAwait(false);
        return _current.Clone();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            _current = settings.Clone();
            var json = JsonSerializer.Serialize(_current, JsonOptions);
            await File.WriteAllTextAsync(_settingsPath, json).ConfigureAwait(false);
            _logger?.LogInformation("Saved dashboard settings to {Path}", _settingsPath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save settings");
            throw;
        }
        finally
        {
            _gate.Release();
        }

        SettingsChanged?.Invoke(this, _current.Clone());
    }

    private async Task EnsureLoadedAsync()
    {
        if (_isLoaded) return;

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_isLoaded) return;

            if (File.Exists(_settingsPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_settingsPath).ConfigureAwait(false);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                    if (loaded != null)
                    {
                        _current = loaded;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to parse settings file, reverting to defaults");
                    _current = new AppSettings();
                }
            }
            else
            {
                _current = new AppSettings();
            }

            _isLoaded = true;
        }
        finally
        {
            _gate.Release();
        }
    }
}

