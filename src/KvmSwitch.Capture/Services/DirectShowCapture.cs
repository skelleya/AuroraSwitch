using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using DirectShowLib;
using KvmSwitch.Capture.Interfaces;
using Microsoft.Extensions.Logging;

namespace KvmSwitch.Capture.Services;

/// <summary>
/// DirectShow-based video capture implementation.
/// </summary>
public class DirectShowCapture : IVideoCapture
{
    private readonly ILogger<DirectShowCapture>? _logger;
    private string _deviceId = string.Empty;
    private bool _isCapturing;
    
    private IGraphBuilder? _graphBuilder;
    private ICaptureGraphBuilder2? _captureGraphBuilder;
    private IBaseFilter? _captureFilter;
    private IBaseFilter? _sampleGrabberFilter;
    private ISampleGrabber? _sampleGrabber;
    private IMediaControl? _mediaControl;
    private IntPtr _frameBuffer = IntPtr.Zero;
    private int _frameWidth;
    private int _frameHeight;
    private int _frameStride;

    public string DeviceId => _deviceId;
    public bool IsCapturing => _isCapturing;

    public event EventHandler<FrameCapturedEventArgs>? FrameCaptured;

    public DirectShowCapture(ILogger<DirectShowCapture>? logger = null)
    {
        _logger = logger;
    }

    public Task<bool> StartCaptureAsync(string deviceId, int width = 1920, int height = 1080, int fps = 60)
    {
        if (_isCapturing)
        {
            return Task.FromResult(false);
        }

        try
        {
            _deviceId = deviceId;

            // Create filter graph
            _graphBuilder = (IGraphBuilder)new FilterGraph();
            _captureGraphBuilder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();

            // Set filter graph
            _captureGraphBuilder.SetFiltergraph(_graphBuilder);

            // Find capture device
            _captureFilter = FindCaptureDevice(deviceId);
            if (_captureFilter == null)
            {
                _logger?.LogError("Capture device not found: {DeviceId}", deviceId);
                return Task.FromResult(false);
            }

            // Add capture filter to graph
            int hr = _graphBuilder.AddFilter(_captureFilter, "Capture Filter");
            if (hr < 0)
            {
                _logger?.LogError("Failed to add capture filter. HR: {HResult}", hr);
                return Task.FromResult(false);
            }

            // Create sample grabber
            _sampleGrabberFilter = (IBaseFilter)new SampleGrabber();
            _sampleGrabber = (ISampleGrabber)_sampleGrabberFilter;

            // Configure sample grabber for RGB24 format
            var mediaType = new AMMediaType
            {
                majorType = MediaType.Video,
                subType = MediaSubType.RGB24,
                formatType = FormatType.VideoInfo
            };

            hr = _sampleGrabber.SetMediaType(mediaType);
            if (hr < 0)
            {
                _logger?.LogError("Failed to set media type. HR: {HResult}", hr);
                return Task.FromResult(false);
            }

            // Add sample grabber to graph
            hr = _graphBuilder.AddFilter(_sampleGrabberFilter, "Sample Grabber");
            if (hr < 0)
            {
                _logger?.LogError("Failed to add sample grabber. HR: {HResult}", hr);
                return Task.FromResult(false);
            }

            // Render capture pin
            hr = _captureGraphBuilder.RenderStream(PinCategory.Capture, MediaType.Video, _captureFilter, null, _sampleGrabberFilter);
            if (hr < 0)
            {
                _logger?.LogError("Failed to render stream. HR: {HResult}", hr);
                return Task.FromResult(false);
            }

            // Set callback for sample grabber
            var callback = new SampleGrabberCallback(this);
            hr = _sampleGrabber.SetCallback(callback, 1); // 1 = SampleCB
            if (hr < 0)
            {
                _logger?.LogError("Failed to set callback. HR: {HResult}", hr);
                return Task.FromResult(false);
            }

            // Get media control
            _mediaControl = (IMediaControl)_graphBuilder;

            // Run graph
            hr = _mediaControl.Run();
            if (hr < 0)
            {
                _logger?.LogError("Failed to run graph. HR: {HResult}", hr);
                return Task.FromResult(false);
            }

            // Get video info header to determine actual resolution
            var videoInfo = new VideoInfoHeader();
            IntPtr ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(VideoInfoHeader)));
            try
            {
                hr = _sampleGrabber.GetConnectedMediaType(mediaType);
                if (hr >= 0 && mediaType.formatPtr != IntPtr.Zero)
                {
                    videoInfo = (VideoInfoHeader)Marshal.PtrToStructure(mediaType.formatPtr, typeof(VideoInfoHeader))!;
                    _frameWidth = videoInfo.BmiHeader.Width;
                    _frameHeight = videoInfo.BmiHeader.Height;
                    _frameStride = _frameWidth * 3; // RGB24 = 3 bytes per pixel
                }
            }
            finally
            {
                if (mediaType.formatPtr != IntPtr.Zero)
                {
                    DsUtils.FreeAMMediaType(mediaType);
                }
                Marshal.FreeCoTaskMem(ptr);
            }

            _isCapturing = true;
            _logger?.LogInformation("Started video capture from device: {DeviceId} ({Width}x{Height})", 
                deviceId, _frameWidth, _frameHeight);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting video capture");
            StopCaptureAsync().Wait();
            return Task.FromResult(false);
        }
    }

    public Task StopCaptureAsync()
    {
        if (!_isCapturing)
        {
            return Task.CompletedTask;
        }

        try
        {
            _mediaControl?.Stop();

            if (_sampleGrabber != null)
            {
                _sampleGrabber.SetCallback(null, 0);
            }

            if (_graphBuilder != null)
            {
                // Remove filters
                if (_sampleGrabberFilter != null)
                {
                    _graphBuilder.RemoveFilter(_sampleGrabberFilter);
                }
                if (_captureFilter != null)
                {
                    _graphBuilder.RemoveFilter(_captureFilter);
                }
            }

            _sampleGrabber = null;
            _sampleGrabberFilter = null;
            _captureFilter = null;
            _captureGraphBuilder = null;
            _mediaControl = null;
            _graphBuilder = null;

            if (_frameBuffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(_frameBuffer);
                _frameBuffer = IntPtr.Zero;
            }

            _isCapturing = false;
            _logger?.LogInformation("Stopped video capture from device: {DeviceId}", _deviceId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping video capture");
        }

        return Task.CompletedTask;
    }

    public Task<Bitmap?> CaptureFrameAsync()
    {
        if (!_isCapturing || _frameBuffer == IntPtr.Zero)
        {
            return Task.FromResult<Bitmap?>(null);
        }

        try
        {
            var bitmap = new Bitmap(_frameWidth, _frameHeight, PixelFormat.Format24bppRgb);
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, _frameWidth, _frameHeight),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

            // Copy frame data to bitmap
            var srcPtr = _frameBuffer;
            var dstPtr = bitmapData.Scan0;
            var bytesToCopy = _frameStride * _frameHeight;

            unsafe
            {
                Buffer.MemoryCopy(
                    (void*)srcPtr,
                    (void*)dstPtr,
                    bytesToCopy,
                    bytesToCopy);
            }

            bitmap.UnlockBits(bitmapData);
            return Task.FromResult<Bitmap?>(bitmap);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error capturing frame");
            return Task.FromResult<Bitmap?>(null);
        }
    }

    private IBaseFilter? FindCaptureDevice(string deviceId)
    {
        // Enumerate video capture devices
        var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
        
        foreach (DsDevice device in devices)
        {
            if (device.DevicePath == deviceId || device.Name.Contains(deviceId))
            {
                var filter = CreateFilterFromDevice(device);
                return filter;
            }
        }

        // If not found by ID, try to use first available device
        if (devices.Length > 0)
        {
            _logger?.LogWarning("Device {DeviceId} not found, using first available device", deviceId);
            return CreateFilterFromDevice(devices[0]);
        }

        return null;
    }

    private IBaseFilter CreateFilterFromDevice(DsDevice device)
    {
        object? sourceObject = null;
        Guid filterGuid = typeof(IBaseFilter).GUID;
        
        try
        {
            if (device.Mon != null)
            {
                device.Mon.BindToObject(null, null, ref filterGuid, out sourceObject);
            }
            
            if (sourceObject == null)
            {
                throw new InvalidOperationException("Failed to create filter from device");
            }
            
            return (IBaseFilter)sourceObject;
        }
        catch
        {
            throw;
        }
    }

    internal void OnFrameReceived(IntPtr buffer, int length)
    {
        if (!_isCapturing || buffer == IntPtr.Zero)
        {
            return;
        }

        try
        {
            // Allocate or reallocate buffer if needed
            var requiredSize = _frameStride * _frameHeight;
            if (_frameBuffer == IntPtr.Zero || requiredSize != length)
            {
                if (_frameBuffer != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(_frameBuffer);
                }
                _frameBuffer = Marshal.AllocCoTaskMem(requiredSize);
            }

            // Copy frame data
            unsafe
            {
                Buffer.MemoryCopy(
                    (void*)buffer,
                    (void*)_frameBuffer,
                    requiredSize,
                    Math.Min(requiredSize, length));
            }

            // Fire event
            FrameCaptured?.Invoke(this, new FrameCapturedEventArgs
            {
                FrameData = _frameBuffer,
                Width = _frameWidth,
                Height = _frameHeight,
                Stride = _frameStride,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing frame");
        }
    }

    public void Dispose()
    {
        StopCaptureAsync().Wait();
    }

    private class SampleGrabberCallback : ISampleGrabberCB
    {
        private readonly DirectShowCapture _parent;

        public SampleGrabberCallback(DirectShowCapture parent)
        {
            _parent = parent;
        }

        public int SampleCB(double sampleTime, IMediaSample pSample)
        {
            try
            {
                pSample.GetPointer(out IntPtr buffer);
                int length = pSample.GetActualDataLength();
                _parent.OnFrameReceived(buffer, length);
            }
            catch
            {
                // Ignore errors in callback
            }
            return 0;
        }

        public int BufferCB(double sampleTime, IntPtr pBuffer, int bufferLen)
        {
            try
            {
                _parent.OnFrameReceived(pBuffer, bufferLen);
            }
            catch
            {
                // Ignore errors in callback
            }
            return 0;
        }
    }
}

