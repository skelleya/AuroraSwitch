using KvmSwitch.Core.Interfaces;
using KvmSwitch.Core.Models;
using Microsoft.Extensions.Logging;

namespace KvmSwitch.Core.Services;

/// <summary>
/// Sets up default hotkey mappings for the KVM system.
/// </summary>
public class DefaultHotkeySetup
{
    private readonly IHotkeyManager _hotkeyManager;
    private readonly IEndpointRegistry _endpointRegistry;
    private readonly ILogger<DefaultHotkeySetup>? _logger;

    public DefaultHotkeySetup(
        IHotkeyManager hotkeyManager,
        IEndpointRegistry endpointRegistry,
        ILogger<DefaultHotkeySetup>? logger = null)
    {
        _hotkeyManager = hotkeyManager;
        _endpointRegistry = endpointRegistry;
        _logger = logger;
    }

    public async Task SetupDefaultsAsync()
    {
        var existingHotkeys = await _hotkeyManager.GetRegisteredHotkeysAsync();
        if (existingHotkeys.Any())
        {
            _logger?.LogInformation("Hotkeys already configured, skipping default setup");
            return;
        }

        _logger?.LogInformation("Setting up default hotkeys");

        // Default hotkey: Ctrl+F1 for host
        var hostHotkey = new HotkeyMapping
        {
            Id = Guid.NewGuid().ToString(),
            Modifiers = ModifierKeys.Ctrl,
            KeyCode = 112, // F1
            EndpointId = "host",
            IsGlobal = true
        };
        await _hotkeyManager.RegisterHotkeyAsync(hostHotkey);
        _logger?.LogInformation("Registered default hotkey: Ctrl+F1 -> Host");

        // Assign Ctrl+F2, F3, etc. to discovered endpoints
        var endpoints = (await _endpointRegistry.GetAllEndpointsAsync())
            .Where(e => e.Id != "host" && e.Type != EndpointType.Host)
            .Take(9) // F2-F10
            .ToList();

        for (int i = 0; i < endpoints.Count; i++)
        {
            var endpoint = endpoints[i];
            var hotkey = new HotkeyMapping
            {
                Id = Guid.NewGuid().ToString(),
                Modifiers = ModifierKeys.Ctrl,
                KeyCode = 112 + i + 1, // F2, F3, F4, etc.
                EndpointId = endpoint.Id,
                IsGlobal = true
            };
            await _hotkeyManager.RegisterHotkeyAsync(hotkey);
            _logger?.LogInformation("Registered default hotkey: Ctrl+F{Key} -> {EndpointName}", 
                i + 2, endpoint.Name);
        }

        // Emergency hotkey: Ctrl+Shift+F12
        var emergencyHotkey = new HotkeyMapping
        {
            Id = Guid.NewGuid().ToString(),
            Modifiers = ModifierKeys.Ctrl | ModifierKeys.Shift,
            KeyCode = 123, // F12
            EndpointId = "host",
            IsGlobal = true
        };
        await _hotkeyManager.RegisterHotkeyAsync(emergencyHotkey);
        _logger?.LogInformation("Registered emergency hotkey: Ctrl+Shift+F12 -> Host");
    }
}

