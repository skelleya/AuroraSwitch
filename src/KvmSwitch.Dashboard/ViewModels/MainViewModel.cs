using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KvmSwitch.Core.Interfaces;
using KvmSwitch.Core.Models;
using KvmSwitch.Capture.Interfaces;
using KvmSwitch.Routing.Interfaces;

namespace KvmSwitch.Dashboard.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IEndpointRegistry _endpointRegistry;
    private readonly IPeripheralRouter _peripheralRouter;
    private readonly IVideoCapture? _videoCapture;
    private readonly IDeviceDiscovery _deviceDiscovery;

    [ObservableProperty]
    private ObservableCollection<Endpoint> _endpoints = new();

    [ObservableProperty]
    private Endpoint? _selectedEndpoint;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _activeEndpointText = "Active: None";

    [ObservableProperty]
    private bool _isVideoVisible;

    [ObservableProperty]
    private BitmapSource? _videoFrameSource;

    [ObservableProperty]
    private string _videoPlaceholderText = "Select an endpoint to view video feed";

    private readonly IHotkeyManager? _hotkeyManager;

    public MainViewModel(
        IEndpointRegistry endpointRegistry,
        IPeripheralRouter peripheralRouter,
        IVideoCapture videoCapture,
        IDeviceDiscovery deviceDiscovery,
        IHotkeyManager? hotkeyManager = null)
    {
        _endpointRegistry = endpointRegistry;
        _peripheralRouter = peripheralRouter;
        _videoCapture = videoCapture;
        _deviceDiscovery = deviceDiscovery;
        _hotkeyManager = hotkeyManager;

        _endpointRegistry.EndpointAdded += OnEndpointAdded;
        _endpointRegistry.EndpointRemoved += OnEndpointRemoved;
        _endpointRegistry.EndpointUpdated += OnEndpointUpdated;
        _peripheralRouter.ActiveEndpointChanged += OnActiveEndpointChanged;
        
        if (_hotkeyManager != null)
        {
            _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
        }

        LoadEndpoints();
        _ = DiscoverInitialDevicesAsync();
    }

    private async void OnHotkeyPressed(object? sender, Core.Interfaces.HotkeyPressedEventArgs e)
    {
        if (e.EndpointId == "host")
        {
            await SwitchToHost();
        }
        else
        {
            var endpoint = await _endpointRegistry.GetEndpointByIdAsync(e.EndpointId);
            if (endpoint != null)
            {
                await SwitchToEndpoint(endpoint);
            }
            else
            {
                // Endpoint doesn't exist - show message
                StatusText = $"Endpoint {e.EndpointId} not found";
                SelectedEndpoint = null;
                await StopVideoCapture();
                VideoFrameSource = null;
                IsVideoVisible = false;
                VideoPlaceholderText = "Endpoint not found\n\nThis endpoint is not available.\nPlease refresh devices.";
            }
        }
    }

    private async void LoadEndpoints()
    {
        var allEndpoints = await _endpointRegistry.GetAllEndpointsAsync();
        Endpoints.Clear();
        foreach (var endpoint in allEndpoints)
        {
            Endpoints.Add(endpoint);
        }
    }

    private void OnEndpointAdded(object? sender, Endpoint endpoint)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            Endpoints.Add(endpoint);
        });
    }

    private void OnEndpointRemoved(object? sender, Endpoint endpoint)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            var toRemove = Endpoints.FirstOrDefault(e => e.Id == endpoint.Id);
            if (toRemove != null)
            {
                Endpoints.Remove(toRemove);
            }
        });
    }

    private void OnEndpointUpdated(object? sender, Endpoint endpoint)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            var existing = Endpoints.FirstOrDefault(e => e.Id == endpoint.Id);
            if (existing != null)
            {
                var index = Endpoints.IndexOf(existing);
                Endpoints[index] = endpoint;
            }
        });
    }

    private void OnActiveEndpointChanged(object? sender, string endpointId)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            ActiveEndpointText = endpointId == "host" ? "Active: Host" : $"Active: {endpointId}";
        });
    }

    [RelayCommand]
    private async Task SwitchToEndpoint(Endpoint endpoint)
    {
        if (endpoint == null) return;

        // If switching to host, handle it specially
        if (endpoint.Id == "host" || endpoint.Type == EndpointType.Host)
        {
            await SwitchToHost();
            return;
        }

        StatusText = $"Switching to {endpoint.Name}...";
        
        // For HDMI endpoints, device connection check is less strict
        // HDMI capture devices are checked separately during video capture
        bool requiresDeviceId = endpoint.ConnectionType != ConnectionType.Hdmi;
        if (endpoint.Status == EndpointStatus.Disconnected && requiresDeviceId && string.IsNullOrEmpty(endpoint.DeviceId))
        {
            StatusText = $"{endpoint.Name} is not connected";
            SelectedEndpoint = endpoint;
            await StopVideoCapture();
            VideoFrameSource = null;
            IsVideoVisible = false;
            VideoPlaceholderText = $"{endpoint.Name}\n\nDevice not connected\n\nPlease connect the device and refresh";
            return;
        }

        // Try to switch with timeout handling
        bool switchSuccess = false;
        bool switchTimedOut = false;
        try
        {
            // Run the switch on a background thread so the UI remains responsive
            var switchTask = Task.Run(async () =>
            {
                try
                {
                    return await _peripheralRouter.SwitchToEndpointAsync(endpoint.Id);
                }
                catch
                {
                    throw;
                }
            });

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(8));
            var completedTask = await Task.WhenAny(switchTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                switchTimedOut = true;
                StatusText = $"{endpoint.Name} - Switch timeout (still trying video capture)";
                // Don't await switchTask here; it may still be running in the background
            }
            else
            {
                switchSuccess = await switchTask;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"{endpoint.Name} - Error: {ex.Message}";
        }
        
        if (!switchSuccess && !switchTimedOut && endpoint.ConnectionType != ConnectionType.Hdmi)
        {
            StatusText = $"{endpoint.Name} did not respond - returning to host";
            await SwitchToHost();
            return;
        }

        // If switch succeeded or timed out, try video capture anyway
        // For HDMI endpoints, always try video capture
        if (switchSuccess || switchTimedOut || endpoint.ConnectionType == ConnectionType.Hdmi)
        {
            SelectedEndpoint = endpoint;
            if (switchSuccess)
            {
                StatusText = $"Connected to {endpoint.Name}";
            }
            else if (switchTimedOut)
            {
                StatusText = $"{endpoint.Name} - Video mode (switch timeout)";
            }
            else
            {
                StatusText = $"Switched to {endpoint.Name} (video mode)";
            }
            await StartVideoCapture(endpoint);
        }
        else
        {
            // Switch failed - check if we can still show video
            if (endpoint.CaptureDeviceIds.Count > 0)
            {
                // Try video capture anyway
                SelectedEndpoint = endpoint;
                StatusText = $"{endpoint.Name} - Video only (USB connection unavailable)";
                await StartVideoCapture(endpoint);
            }
            else
            {
                StatusText = $"{endpoint.Name} is not available";
                SelectedEndpoint = endpoint;
                await StopVideoCapture();
                VideoFrameSource = null;
                IsVideoVisible = false;
                VideoPlaceholderText = $"{endpoint.Name}\n\nDevice not available\n\nUnable to connect to device.\nPlease check connection and refresh.";
            }
        }
    }

    [RelayCommand]
    private async Task SwitchToHost()
    {
        StatusText = "Switching to host...";
        var success = await _peripheralRouter.SwitchToHostAsync();
        
        if (success)
        {
            SelectedEndpoint = null;
            StatusText = "Connected to host";
            await StopVideoCapture();
            VideoFrameSource = null;
            IsVideoVisible = false;
            VideoPlaceholderText = "Host PC\n\nSelect an endpoint to view video feed";
        }
        else
        {
            StatusText = "Failed to switch to host";
        }
    }

    [RelayCommand]
    private async Task RefreshDevices()
    {
        StatusText = "Scanning for devices...";
        try
        {
            var discovered = (await _deviceDiscovery.ScanForDevicesAsync()).ToList();
            foreach (var endpoint in discovered)
            {
                await _endpointRegistry.AddOrUpdateEndpointAsync(endpoint);
            }
            await _endpointRegistry.SaveAsync();
            LoadEndpoints();
            StatusText = $"Devices refreshed ({discovered.Count} detected)";
        }
        catch (Exception ex)
        {
            StatusText = $"Discovery error: {ex.Message}";
        }
    }

    private async Task StartVideoCapture(Endpoint endpoint)
    {
        if (endpoint.CaptureDeviceIds.Count == 0)
        {
            IsVideoVisible = false;
            VideoFrameSource = null;
            VideoPlaceholderText = $"{endpoint.Name}\n\nNo video capture device configured\nor device not connected";
            StatusText = $"{endpoint.Name} - No video capture device configured";
            return;
        }

        try
        {
            var deviceId = endpoint.CaptureDeviceIds.First();
            var started = await _videoCapture.StartCaptureAsync(deviceId);
            
            if (started)
            {
                IsVideoVisible = true;
                _videoCapture.FrameCaptured += OnFrameCaptured;
            }
            else
            {
                IsVideoVisible = false;
                VideoFrameSource = null;
                VideoPlaceholderText = $"{endpoint.Name}\n\nVideo capture device not available\n\nPlease check capture device connection";
                StatusText = $"{endpoint.Name} - Video capture device not available";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Video capture error: {ex.Message}";
            IsVideoVisible = false;
            VideoFrameSource = null;
        }
    }

    private void OnFrameCaptured(object? sender, Capture.Interfaces.FrameCapturedEventArgs e)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                // Convert frame data to BitmapSource for WPF display
                var bitmapSource = Helpers.BitmapConverter.ToBitmapSource(
                    e.FrameData, 
                    e.Width, 
                    e.Height, 
                    e.Stride);

                VideoFrameSource = bitmapSource;
            }
            catch (Exception ex)
            {
                StatusText = $"Video error: {ex.Message}";
            }
        });
    }

    private async Task StopVideoCapture()
    {
        if (_videoCapture.IsCapturing)
        {
            _videoCapture.FrameCaptured -= OnFrameCaptured;
            await _videoCapture.StopCaptureAsync();
        }
        IsVideoVisible = false;
    }

    private async Task DiscoverInitialDevicesAsync()
    {
        try
        {
            var discovered = (await _deviceDiscovery.ScanForDevicesAsync()).ToList();
            foreach (var endpoint in discovered)
            {
                await _endpointRegistry.AddOrUpdateEndpointAsync(endpoint);
            }
            if (discovered.Count > 0)
            {
                await _endpointRegistry.SaveAsync();
                LoadEndpoints();
                StatusText = $"Detected {discovered.Count} endpoints";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Discovery error: {ex.Message}";
        }
    }
}

