using System.Drawing;

namespace KvmSwitch.Capture.Interfaces;

/// <summary>
/// Captures video from a capture device (HDMI/USB-C capture card).
/// </summary>
public interface IVideoCapture : IDisposable
{
    string DeviceId { get; }
    bool IsCapturing { get; }
    
    event EventHandler<FrameCapturedEventArgs>? FrameCaptured;
    
    Task<bool> StartCaptureAsync(string deviceId, int width = 1920, int height = 1080, int fps = 60);
    Task StopCaptureAsync();
    Task<Bitmap?> CaptureFrameAsync();
}

public class FrameCapturedEventArgs : EventArgs
{
    public IntPtr FrameData { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Stride { get; set; }
    public DateTime Timestamp { get; set; }
}

