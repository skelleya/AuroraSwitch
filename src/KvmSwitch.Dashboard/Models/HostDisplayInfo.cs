using CommunityToolkit.Mvvm.ComponentModel;

namespace KvmSwitch.Dashboard.Models;

public partial class HostDisplayInfo : ObservableObject
{
    [ObservableProperty] private string _deviceName = string.Empty;
    [ObservableProperty] private string _friendlyName = string.Empty;
    [ObservableProperty] private string _resolution = string.Empty;
    [ObservableProperty] private bool _isPrimary;
    [ObservableProperty] private bool _isProjected;
    [ObservableProperty] private double _left;
    [ObservableProperty] private double _top;
    [ObservableProperty] private double _width;
    [ObservableProperty] private double _height;
}

