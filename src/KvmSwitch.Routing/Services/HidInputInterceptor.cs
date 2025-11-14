using System.Runtime.InteropServices;
using KvmSwitch.Core.Native;
using KvmSwitch.Routing.Interfaces;
using Microsoft.Extensions.Logging;

namespace KvmSwitch.Routing.Services;

/// <summary>
/// Intercepts HID input (keyboard/mouse) on the host and routes it to the active endpoint.
/// </summary>
public class HidInputInterceptor : IDisposable
{
    private readonly IPeripheralRouter _peripheralRouter;
    private readonly ILogger<HidInputInterceptor>? _logger;
    private IntPtr _keyboardHookId = IntPtr.Zero;
    private IntPtr _mouseHookId = IntPtr.Zero;
    private WindowsHooks.LowLevelKeyboardProc? _keyboardHookProc;
    private WindowsHooks.LowLevelMouseProc? _mouseHookProc;
    private bool _isIntercepting;
    private string? _activeEndpointId;
    private WindowsHooks.POINT _lastMousePosition;
    private bool _leftButtonDown;
    private bool _rightButtonDown;
    private bool _middleButtonDown;

    public HidInputInterceptor(IPeripheralRouter peripheralRouter, ILogger<HidInputInterceptor>? logger = null)
    {
        _peripheralRouter = peripheralRouter;
        _logger = logger;
        _keyboardHookProc = KeyboardHookCallback;
        _mouseHookProc = MouseHookCallback;
        
        _peripheralRouter.ActiveEndpointChanged += OnActiveEndpointChanged;
    }

    private void OnActiveEndpointChanged(object? sender, string endpointId)
    {
        _activeEndpointId = endpointId == "host" ? null : endpointId;
        
        // Reset mouse state when switching endpoints
        _leftButtonDown = false;
        _rightButtonDown = false;
        _middleButtonDown = false;
        
        // Get current mouse position as starting point
        WindowsHooks.GetCursorPos(out _lastMousePosition);
        
        if (_activeEndpointId != null && !_isIntercepting)
        {
            StartIntercepting();
        }
        else if (_activeEndpointId == null && _isIntercepting)
        {
            StopIntercepting();
        }
    }

    public void StartIntercepting()
    {
        if (_isIntercepting) return;

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

            // Install keyboard hook
            _keyboardHookId = WindowsHooks.SetWindowsHookEx(
                WindowsHooks.WH_KEYBOARD_LL,
                _keyboardHookProc!,
                moduleHandle,
                0);

            // Install mouse hook
            _mouseHookId = WindowsHooks.SetWindowsHookEx(
                WindowsHooks.WH_MOUSE_LL,
                _mouseHookProc!,
                moduleHandle,
                0);

            if (_keyboardHookId == IntPtr.Zero || _mouseHookId == IntPtr.Zero)
            {
                _logger?.LogError("Failed to install input hooks. Error: {Error}", Marshal.GetLastWin32Error());
                StopIntercepting();
                return;
            }

            _isIntercepting = true;
            _logger?.LogInformation("Started intercepting HID input for endpoint: {EndpointId}", _activeEndpointId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting input interception");
        }
    }

    public void StopIntercepting()
    {
        if (!_isIntercepting) return;

        if (_keyboardHookId != IntPtr.Zero)
        {
            WindowsHooks.UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }

        if (_mouseHookId != IntPtr.Zero)
        {
            WindowsHooks.UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }

        _isIntercepting = false;
        _logger?.LogInformation("Stopped intercepting HID input");
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _activeEndpointId != null)
        {
            // Intercept keyboard input and route to active endpoint
            if (wParam == (IntPtr)WindowsHooks.WM_KEYDOWN || wParam == (IntPtr)WindowsHooks.WM_SYSKEYDOWN)
            {
                var vkCode = Marshal.ReadInt32(lParam);
                var keyData = new byte[] { (byte)vkCode };
                
                // Route to active endpoint asynchronously
                _ = Task.Run(async () =>
                {
                    await _peripheralRouter.SendKeyboardInputAsync(_activeEndpointId, keyData);
                });

                // Suppress the key event on host
                return (IntPtr)1;
            }
        }

        return WindowsHooks.CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _activeEndpointId != null)
        {
            var mouseData = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var message = (int)wParam;
            
            // Handle button states
            switch (message)
            {
                case WindowsHooks.WM_LBUTTONDOWN:
                    _leftButtonDown = true;
                    break;
                case WindowsHooks.WM_LBUTTONUP:
                    _leftButtonDown = false;
                    break;
                case WindowsHooks.WM_RBUTTONDOWN:
                    _rightButtonDown = true;
                    break;
                case WindowsHooks.WM_RBUTTONUP:
                    _rightButtonDown = false;
                    break;
                case WindowsHooks.WM_MBUTTONDOWN:
                    _middleButtonDown = true;
                    break;
                case WindowsHooks.WM_MBUTTONUP:
                    _middleButtonDown = false;
                    break;
            }
            
            // Calculate relative movement (only for mouse move events)
            var deltaX = 0;
            var deltaY = 0;
            if (message == WindowsHooks.WM_MOUSEMOVE)
            {
                deltaX = mouseData.pt.x - _lastMousePosition.x;
                deltaY = mouseData.pt.y - _lastMousePosition.y;
                _lastMousePosition = mouseData.pt;
            }
            
            // Handle mouse wheel
            var scrollDelta = 0;
            if (message == WindowsHooks.WM_MOUSEWHEEL)
            {
                scrollDelta = (short)((mouseData.mouseData >> 16) & 0xFFFF);
            }
            
            // Route mouse input to active endpoint asynchronously
            _ = Task.Run(async () =>
            {
                var buttons = new Core.Interfaces.MouseButtonState
                {
                    LeftButton = _leftButtonDown,
                    RightButton = _rightButtonDown,
                    MiddleButton = _middleButtonDown,
                    ScrollDelta = scrollDelta
                };
                
                await _peripheralRouter.SendMouseInputAsync(_activeEndpointId, deltaX, deltaY, buttons);
            });

            // Suppress the mouse event on host
            return (IntPtr)1;
        }

        return WindowsHooks.CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }


    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public WindowsHooks.POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    public void Dispose()
    {
        StopIntercepting();
        _peripheralRouter.ActiveEndpointChanged -= OnActiveEndpointChanged;
        _keyboardHookProc = null;
        _mouseHookProc = null;
    }
}

