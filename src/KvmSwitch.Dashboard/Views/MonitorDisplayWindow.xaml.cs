using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using KvmSwitch.Dashboard.Models;

namespace KvmSwitch.Dashboard.Views;

public partial class MonitorDisplayWindow : Window
{
    public HostDisplayInfo? TargetDisplay { get; private set; }

    public MonitorDisplayWindow()
    {
        InitializeComponent();
        Loaded += MonitorDisplayWindow_Loaded;
        PreviewKeyDown += MonitorDisplayWindow_PreviewKeyDown;
        MouseDoubleClick += MonitorDisplayWindow_MouseDoubleClick;
    }

    public void SetTargetDisplay(HostDisplayInfo display)
    {
        TargetDisplay = display;
    }

    private void MonitorDisplayWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (TargetDisplay == null)
        {
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        Left = TargetDisplay.Left / dpi.DpiScaleX;
        Top = TargetDisplay.Top / dpi.DpiScaleY;
        Width = TargetDisplay.Width / dpi.DpiScaleX;
        Height = TargetDisplay.Height / dpi.DpiScaleY;
    }

    private void MonitorDisplayWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void MonitorDisplayWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        Close();
    }
}

