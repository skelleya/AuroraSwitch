namespace KvmSwitch.Capture.Interfaces;

/// <summary>
/// Captures audio from a capture device.
/// </summary>
public interface IAudioCapture : IDisposable
{
    string DeviceId { get; }
    bool IsCapturing { get; }
    
    event EventHandler<AudioDataEventArgs>? AudioDataCaptured;
    
    Task<bool> StartCaptureAsync(string deviceId);
    Task StopCaptureAsync();
}

public class AudioDataEventArgs : EventArgs
{
    public byte[] AudioData { get; set; } = Array.Empty<byte>();
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public int BitsPerSample { get; set; }
    public DateTime Timestamp { get; set; }
}

