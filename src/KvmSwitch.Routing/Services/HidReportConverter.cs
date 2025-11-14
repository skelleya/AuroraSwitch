using KvmSwitch.Core.Interfaces;
using KvmSwitch.Core.Models;

namespace KvmSwitch.Routing.Services;

/// <summary>
/// Converts Windows virtual key codes and mouse input to USB HID report format.
/// </summary>
public static class HidReportConverter
{
    // USB HID Usage Page constants
    private const byte HID_USAGE_PAGE_GENERIC_DESKTOP = 0x01;
    private const byte HID_USAGE_KEYBOARD = 0x06;
    private const byte HID_USAGE_MOUSE = 0x02;

    /// <summary>
    /// Converts a Windows virtual key code to USB HID keyboard usage code.
    /// </summary>
    public static byte VirtualKeyToHidUsage(int vkCode)
    {
        // USB HID keyboard usage codes map directly to most VK codes
        // This is a simplified mapping - full implementation would need complete VK to HID mapping
        return vkCode switch
        {
            >= 0x30 and <= 0x39 => (byte)(vkCode - 0x30 + 0x1E), // 0-9
            >= 0x41 and <= 0x5A => (byte)(vkCode - 0x41 + 0x04), // A-Z
            >= 0x70 and <= 0x87 => (byte)(vkCode - 0x70 + 0x3A), // F1-F24
            0x08 => 0x2A, // Backspace
            0x09 => 0x2B, // Tab
            0x0D => 0x28, // Enter
            0x1B => 0x29, // Escape
            0x20 => 0x2C, // Space
            0x25 => 0x50, // Left Arrow
            0x26 => 0x52, // Up Arrow
            0x27 => 0x4F, // Right Arrow
            0x28 => 0x51, // Down Arrow
            0x2D => 0x4A, // Insert
            0x2E => 0x4C, // Delete
            0x21 => 0x4B, // Page Up
            0x22 => 0x4E, // Page Down
            0x24 => 0x4D, // Home
            0x23 => 0x4D, // End
            _ => 0x00 // Unknown/unsupported
        };
    }

    /// <summary>
    /// Creates a USB HID keyboard report (8 bytes).
    /// Format: [Modifier Keys][Reserved][Key1][Key2][Key3][Key4][Key5][Key6]
    /// </summary>
    public static byte[] CreateKeyboardReport(int vkCode, ModifierKeys modifiers)
    {
        var report = new byte[8];
        
        // Modifier byte (bit flags)
        byte modifierByte = 0;
        if ((modifiers & ModifierKeys.Ctrl) != 0) modifierByte |= 0x01;
        if ((modifiers & ModifierKeys.Shift) != 0) modifierByte |= 0x02;
        if ((modifiers & ModifierKeys.Alt) != 0) modifierByte |= 0x04;
        if ((modifiers & ModifierKeys.Win) != 0) modifierByte |= 0x08;
        
        report[0] = modifierByte;
        report[1] = 0x00; // Reserved
        report[2] = VirtualKeyToHidUsage(vkCode); // First key
        // Keys 3-7 are for multiple key presses (not used for single key)
        
        return report;
    }

    /// <summary>
    /// Creates a USB HID mouse report (4 bytes).
    /// Format: [Buttons][DeltaX][DeltaY][Wheel]
    /// </summary>
    public static byte[] CreateMouseReport(int deltaX, int deltaY, MouseButtonState buttons)
    {
        var report = new byte[4];
        
        // Button byte (bit flags)
        byte buttonByte = 0;
        if (buttons.LeftButton) buttonByte |= 0x01;
        if (buttons.RightButton) buttonByte |= 0x02;
        if (buttons.MiddleButton) buttonByte |= 0x04;
        
        report[0] = buttonByte;
        
        // Delta X (signed byte, clamped to -127 to 127)
        report[1] = (byte)Math.Max(-127, Math.Min(127, deltaX));
        
        // Delta Y (signed byte, clamped to -127 to 127)
        report[2] = (byte)Math.Max(-127, Math.Min(127, deltaY));
        
        // Wheel delta (signed byte)
        report[3] = (byte)Math.Max(-127, Math.Min(127, buttons.ScrollDelta));
        
        return report;
    }

    /// <summary>
    /// Creates an empty keyboard report (all keys released).
    /// </summary>
    public static byte[] CreateEmptyKeyboardReport()
    {
        return new byte[8]; // All zeros
    }

    /// <summary>
    /// Creates an empty mouse report (no movement, no buttons).
    /// </summary>
    public static byte[] CreateEmptyMouseReport()
    {
        return new byte[4]; // All zeros
    }
}

