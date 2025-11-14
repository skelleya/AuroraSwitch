using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using KvmSwitch.Core.Interfaces;
using KvmSwitch.Core.Models;
using KvmSwitch.Dashboard.ViewModels;
using KvmSwitch.Dashboard.Views;

namespace KvmSwitch.Dashboard;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly IEndpointRegistry _endpointRegistry;
    private readonly IAppSettingsService _settingsService;
    private readonly IUpdateService _updateService;
    private AppSettings _currentSettings = new();

    public MainWindow(
        MainViewModel viewModel,
        IHotkeyManager hotkeyManager,
        IEndpointRegistry endpointRegistry,
        IAppSettingsService settingsService,
        IUpdateService updateService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _hotkeyManager = hotkeyManager;
        _endpointRegistry = endpointRegistry;
        _settingsService = settingsService;
        _updateService = updateService;
        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        _settingsService.SettingsChanged += SettingsServiceOnSettingsChanged;
        VersionText.Text = $"AuroraSwitch v{_updateService.CurrentVersion}";
        
        // Set window icon
        try
        {
            var iconPath = Path.Combine(System.AppContext.BaseDirectory, "icon.ico");
            if (File.Exists(iconPath))
            {
                using var stream = File.OpenRead(iconPath);
                var decoder = new System.Windows.Media.Imaging.IconBitmapDecoder(
                    stream,
                    System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
                    System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                if (decoder.Frames.Count > 0)
                {
                    this.Icon = decoder.Frames[0];
                }
            }
        }
        catch
        {
            // Icon loading failed, continue without icon
        }
        
        // Bind status bar
        StatusText.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("StatusText") { Source = _viewModel });
        ActiveEndpointText.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("ActiveEndpointText") { Source = _viewModel });
        
        // Handle window state changes for minimize to tray
        this.StateChanged += MainWindow_StateChanged;
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (_currentSettings.EnableSystemTray && WindowState == WindowState.Minimized)
        {
            Hide();
        }
        else if (WindowState == WindowState.Normal)
        {
            Show();
        }
    }

    private void EndpointListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EndpointListBox.SelectedItem is Core.Models.Endpoint endpoint)
        {
            _viewModel.SwitchToEndpointCommand.Execute(endpoint);
        }
    }

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow(_settingsService)
        {
            Owner = this
        };
        window.ShowDialog();
        _currentSettings = await _settingsService.GetAsync();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void RefreshDevices_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.RefreshDevicesCommand.Execute(null);
    }

    private void ManageHotkeys_Click(object sender, RoutedEventArgs e)
    {
        var window = new ManageHotkeysWindow(_hotkeyManager, _endpointRegistry)
        {
            Owner = this
        };
        window.ShowDialog();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("KVM Software Switch v1.0\n\nCustom software KVM solution for managing multiple computers from a single host.", 
            "About", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        var window = new UpdateWindow(_updateService) { Owner = this };
        window.ShowDialog();
    }

    private void PatchNotes_Click(object sender, RoutedEventArgs e)
    {
        var window = new UpdateWindow(_updateService, true) { Owner = this };
        window.ShowDialog();
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _currentSettings = await _settingsService.GetAsync();
        if (_currentSettings.StartMinimized)
        {
            WindowState = WindowState.Minimized;
            if (_currentSettings.EnableSystemTray)
            {
                Hide();
            }
        }
    }

    private void SettingsServiceOnSettingsChanged(object? sender, AppSettings e)
    {
        _currentSettings = e;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_currentSettings.ConfirmOnExit)
        {
            var result = MessageBox.Show(
                "Are you sure you want to exit AuroraSwitch Dashboard?",
                "Confirm Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
        }

        _settingsService.SettingsChanged -= SettingsServiceOnSettingsChanged;
    }
}

