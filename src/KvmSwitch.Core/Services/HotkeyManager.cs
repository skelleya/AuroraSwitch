using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using KvmSwitch.Core.Interfaces;
using KvmSwitch.Core.Models;
using KvmSwitch.Core.Native;
using Microsoft.Extensions.Logging;

namespace KvmSwitch.Core.Services;

public class HotkeyManager : IHotkeyManager, IDisposable
{
    private readonly ConcurrentDictionary<string, HotkeyMapping> _registeredHotkeys = new();
    private readonly ILogger<HotkeyManager>? _logger;
    private readonly HotkeyRegistry? _hotkeyRegistry;
    private IntPtr _hookId = IntPtr.Zero;
    private bool _isListening;
    private WindowsHooks.LowLevelKeyboardProc? _hookProc;

    public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    public HotkeyManager(ILogger<HotkeyManager>? logger = null, HotkeyRegistry? hotkeyRegistry = null)
    {
        _logger = logger;
        _hotkeyRegistry = hotkeyRegistry;
        _hookProc = HookCallback;
    }

    public async Task<bool> RegisterHotkeyAsync(HotkeyMapping mapping)
    {
        if (_registeredHotkeys.TryAdd(mapping.Id, mapping))
        {
            _logger?.LogInformation("Registered hotkey: {Modifiers}+{KeyCode} -> {EndpointId}", 
                mapping.Modifiers, mapping.KeyCode, mapping.EndpointId);
            
            // Save to database
            if (_hotkeyRegistry != null)
            {
                await _hotkeyRegistry.SaveHotkeysAsync(_registeredHotkeys.Values);
            }
            
            return true;
        }
        return false;
    }

    public async Task<bool> UnregisterHotkeyAsync(string hotkeyId)
    {
        if (_registeredHotkeys.TryRemove(hotkeyId, out _))
        {
            _logger?.LogInformation("Unregistered hotkey: {HotkeyId}", hotkeyId);
            
            // Save to database
            if (_hotkeyRegistry != null)
            {
                await _hotkeyRegistry.SaveHotkeysAsync(_registeredHotkeys.Values);
            }
            
            return true;
        }
        return false;
    }

    public async Task LoadHotkeysAsync()
    {
        if (_hotkeyRegistry == null) return;

        var hotkeys = await _hotkeyRegistry.LoadHotkeysAsync();
        _registeredHotkeys.Clear();
        
        foreach (var hotkey in hotkeys)
        {
            _registeredHotkeys.TryAdd(hotkey.Id, hotkey);
        }
        
        _logger?.LogInformation("Loaded {Count} hotkeys from database", hotkeys.Count());
    }

    public Task<IEnumerable<HotkeyMapping>> GetRegisteredHotkeysAsync()
    {
        return Task.FromResult(_registeredHotkeys.Values.AsEnumerable());
    }

    public Task<bool> IsHotkeyAvailableAsync(ModifierKeys modifiers, int keyCode)
    {
        var conflict = _registeredHotkeys.Values.FirstOrDefault(h => 
            h.Modifiers == modifiers && h.KeyCode == keyCode);
        return Task.FromResult(conflict == null);
    }

    public void StartListening()
    {
        if (_isListening) return;
        
        try
        {
            using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            if (curModule == null)
            {
                _logger?.LogError("Failed to get main module for hook installation");
                return;
            }

            var moduleHandle = WindowsHooks.GetModuleHandle(curModule.ModuleName);
            if (moduleHandle == IntPtr.Zero)
            {
                _logger?.LogError("Failed to get module handle. Error: {Error}", Marshal.GetLastWin32Error());
                return;
            }

            _hookId = WindowsHooks.SetWindowsHookEx(
                WindowsHooks.WH_KEYBOARD_LL,
                _hookProc!,
                moduleHandle,
                0);

            if (_hookId == IntPtr.Zero)
            {
                _logger?.LogError("Failed to install keyboard hook. Error: {Error}", Marshal.GetLastWin32Error());
                return;
            }

            _isListening = true;
            _logger?.LogInformation("Hotkey manager started listening");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting hotkey listener");
        }
    }

    public void StopListening()
    {
        if (!_isListening) return;
        
        if (_hookId != IntPtr.Zero)
        {
            WindowsHooks.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        
        _isListening = false;
        _logger?.LogInformation("Hotkey manager stopped listening");
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            // Check for key down events
            if (wParam == (IntPtr)WindowsHooks.WM_KEYDOWN || wParam == (IntPtr)WindowsHooks.WM_SYSKEYDOWN)
            {
                var vkCode = Marshal.ReadInt32(lParam);
                
                // Get current modifier state
                var modifiers = GetCurrentModifiers();
                
                // Check if this matches any registered hotkey
                var matchingHotkey = _registeredHotkeys.Values.FirstOrDefault(h =>
                    h.KeyCode == vkCode && h.Modifiers == modifiers);

                if (matchingHotkey != null)
                {
                    _logger?.LogDebug("Hotkey pressed: {Modifiers}+{KeyCode} -> {EndpointId}",
                        modifiers, vkCode, matchingHotkey.EndpointId);

                    HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs
                    {
                        EndpointId = matchingHotkey.EndpointId,
                        Modifiers = modifiers,
                        KeyCode = vkCode
                    });

                    // Return non-zero to suppress the key event
                    return (IntPtr)1;
                }
            }
        }

        return WindowsHooks.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private ModifierKeys GetCurrentModifiers()
    {
        var modifiers = ModifierKeys.None;

        // Check Control keys
        if ((WindowsHooks.GetAsyncKeyState(WindowsHooks.VK_LCONTROL) & 0x8000) != 0 ||
            (WindowsHooks.GetAsyncKeyState(WindowsHooks.VK_RCONTROL) & 0x8000) != 0)
        {
            modifiers |= ModifierKeys.Ctrl;
        }

        // Check Alt keys
        if ((WindowsHooks.GetAsyncKeyState(WindowsHooks.VK_LMENU) & 0x8000) != 0 ||
            (WindowsHooks.GetAsyncKeyState(WindowsHooks.VK_RMENU) & 0x8000) != 0)
        {
            modifiers |= ModifierKeys.Alt;
        }

        // Check Shift keys
        if ((WindowsHooks.GetAsyncKeyState(WindowsHooks.VK_LSHIFT) & 0x8000) != 0 ||
            (WindowsHooks.GetAsyncKeyState(WindowsHooks.VK_RSHIFT) & 0x8000) != 0)
        {
            modifiers |= ModifierKeys.Shift;
        }

        // Check Windows keys
        if ((WindowsHooks.GetAsyncKeyState(WindowsHooks.VK_LWIN) & 0x8000) != 0 ||
            (WindowsHooks.GetAsyncKeyState(WindowsHooks.VK_RWIN) & 0x8000) != 0)
        {
            modifiers |= ModifierKeys.Win;
        }

        return modifiers;
    }

    public void Dispose()
    {
        StopListening();
        _hookProc = null;
    }
}

