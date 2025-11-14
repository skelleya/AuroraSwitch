using DirectShowLib;

namespace KvmSwitch.Capture.Services;

/// <summary>
/// Enumerates available video capture devices.
/// </summary>
public static class CaptureDeviceEnumerator
{
    /// <summary>
    /// Gets all available video capture devices.
    /// </summary>
    public static List<CaptureDeviceInfo> GetCaptureDevices()
    {
        var devices = new List<CaptureDeviceInfo>();

        try
        {
            var dsDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            for (int i = 0; i < dsDevices.Length; i++)
            {
                var dsDevice = dsDevices[i];
                var monikerString = string.Empty;

                try
                {
                    dsDevice.Mon?.GetDisplayName(null, null, out monikerString);
                }
                catch
                {
                    monikerString = string.Empty;
                }

                devices.Add(new CaptureDeviceInfo
                {
                    Index = i,
                    Name = dsDevice.Name,
                    DevicePath = dsDevice.DevicePath,
                    MonikerString = monikerString
                });
            }
        }
        catch (Exception)
        {
            // Return empty list on error
        }

        return devices;
    }

    /// <summary>
    /// Finds a capture device by name or path.
    /// </summary>
    public static CaptureDeviceInfo? FindDevice(string deviceId)
    {
        var devices = GetCaptureDevices();
        return devices.FirstOrDefault(d =>
            d.Name.Contains(deviceId, StringComparison.OrdinalIgnoreCase) ||
            d.DevicePath == deviceId ||
            d.MonikerString == deviceId);
    }
}

public class CaptureDeviceInfo
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DevicePath { get; set; } = string.Empty;
    public string MonikerString { get; set; } = string.Empty;
}

