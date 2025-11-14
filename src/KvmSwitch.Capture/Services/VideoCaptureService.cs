using KvmSwitch.Capture.Interfaces;
using Microsoft.Extensions.Logging;

namespace KvmSwitch.Capture.Services;

/// <summary>
/// Wrapper service that uses DirectShowCapture for video capture.
/// </summary>
public class VideoCaptureService : IVideoCapture
{
    private readonly DirectShowCapture _directShowCapture;
    private readonly ILogger<VideoCaptureService>? _logger;

    public string DeviceId => _directShowCapture.DeviceId;
    public bool IsCapturing => _directShowCapture.IsCapturing;

    public event EventHandler<FrameCapturedEventArgs>? FrameCaptured
    {
        add => _directShowCapture.FrameCaptured += value;
        remove => _directShowCapture.FrameCaptured -= value;
    }

    public VideoCaptureService(ILoggerFactory? loggerFactory = null)
    {
        _logger = loggerFactory?.CreateLogger<VideoCaptureService>();
        _directShowCapture = new DirectShowCapture(loggerFactory?.CreateLogger<DirectShowCapture>());
    }

    public Task<bool> StartCaptureAsync(string deviceId, int width = 1920, int height = 1080, int fps = 60)
    {
        return _directShowCapture.StartCaptureAsync(deviceId, width, height, fps);
    }

    public Task StopCaptureAsync()
    {
        return _directShowCapture.StopCaptureAsync();
    }

    public Task<System.Drawing.Bitmap?> CaptureFrameAsync()
    {
        return _directShowCapture.CaptureFrameAsync();
    }

    public void Dispose()
    {
        _directShowCapture.Dispose();
    }
}

