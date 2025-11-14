using System;
using System.Windows;
using System.Windows.Controls;
using KvmSwitch.Core.Interfaces;
using KvmSwitch.Core.Models;

namespace KvmSwitch.Dashboard.Views;

public partial class SettingsWindow : Window
{
    private readonly IAppSettingsService _settingsService;
    private AppSettings _settings = new();

    public SettingsWindow(IAppSettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        Loaded += SettingsWindow_Loaded;
    }

    private async void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _settings = await _settingsService.GetAsync();
        StartMinimizedCheck.IsChecked = _settings.StartMinimized;
        EnableTrayCheck.IsChecked = _settings.EnableSystemTray;
        ConfirmExitCheck.IsChecked = _settings.ConfirmOnExit;
        foreach (ComboBoxItem item in ThemeCombo.Items)
        {
            if (string.Equals(item.Content?.ToString(), _settings.Theme, StringComparison.OrdinalIgnoreCase))
            {
                ThemeCombo.SelectedItem = item;
                break;
            }
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.StartMinimized = StartMinimizedCheck.IsChecked == true;
        _settings.EnableSystemTray = EnableTrayCheck.IsChecked == true;
        _settings.ConfirmOnExit = ConfirmExitCheck.IsChecked == true;
        if (ThemeCombo.SelectedItem is ComboBoxItem item && item.Content is string theme)
        {
            _settings.Theme = theme;
        }

        await _settingsService.SaveAsync(_settings);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

