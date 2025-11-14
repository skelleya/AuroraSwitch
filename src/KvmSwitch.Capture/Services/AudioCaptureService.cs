using KvmSwitch.Capture.Interfaces;
using Microsoft.Extensions.Logging;

namespace KvmSwitch.Capture.Services;

public class AudioCaptureService : IAudioCapture
{
    private readonly ILogger<AudioCaptureService>? _logger;
    private string _deviceId = string.Empty;
    private bool _isCapturing;

    public string DeviceId => _deviceId;
    public bool IsCapturing => _isCapturing;

    public event EventHandler<AudioDataEventArgs>? AudioDataCaptured;

    public AudioCaptureService(ILogger<AudioCaptureService>? logger = null)
    {
        _logger = logger;
    }

    public Task<bool> StartCaptureAsync(string deviceId)
    {
        if (_isCapturing)
        {
            return Task.FromResult(false);
        }

        _deviceId = deviceId;
        _isCapturing = true;
        
        // TODO: Initialize WASAPI or DirectShow audio capture
        _logger?.LogInformation("Starting audio capture from device: {DeviceId}", deviceId);
        
        return Task.FromResult(true);
    }

    public Task StopCaptureAsync()
    {
        if (!_isCapturing)
        {
            return Task.CompletedTask;
        }

        _isCapturing = false;
        
        // TODO: Stop and release audio capture
        _logger?.LogInformation("Stopped audio capture from device: {DeviceId}", _deviceId);
        
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopCaptureAsync().Wait();
    }
}

