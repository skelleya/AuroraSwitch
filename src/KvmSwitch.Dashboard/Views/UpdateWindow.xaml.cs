using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using KvmSwitch.Core.Interfaces;

namespace KvmSwitch.Dashboard.Views;

public partial class UpdateWindow : Window
{
    private readonly IUpdateService _updateService;
    private readonly bool _showEvenIfCurrent;
    private UpdateCheckResult? _lastResult;

    public UpdateWindow(IUpdateService updateService, bool showEvenIfCurrent = false)
    {
        InitializeComponent();
        _updateService = updateService;
        _showEvenIfCurrent = showEvenIfCurrent;
        Loaded += UpdateWindow_Loaded;
    }

    private async void UpdateWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await CheckAsync();
    }

    private async Task CheckAsync()
    {
        StatusText.Text = "Checking for updates...";
        var result = await _updateService.CheckForUpdatesAsync();
        _lastResult = result;

        CurrentVersionText.Text = result.CurrentVersion.ToString();
        LatestVersionText.Text = result.LatestVersion.ToString();

        if (!result.IsSuccess)
        {
            StatusText.Text = $"Update check failed: {result.ErrorMessage}";
            return;
        }

        if (!result.IsUpdateAvailable && !_showEvenIfCurrent)
        {
            StatusText.Text = "You already have the latest version.";
        }
        else if (!result.IsUpdateAvailable && _showEvenIfCurrent)
        {
            StatusText.Text = "Showing latest release information.";
        }
        else
        {
            StatusText.Text = "A new update is available!";
        }

        NotesList.ItemsSource = result.Manifest?.Notes ?? new List<string>();
        if (result.Manifest?.PublishedAt is { } published)
        {
            PublishedText.Text = $"Published: {published:MMMM dd, yyyy}";
        }
        else
        {
            PublishedText.Text = string.Empty;
        }

        var hasDownload = !string.IsNullOrWhiteSpace(result.Manifest?.DownloadUrl);
        DownloadButton.IsEnabled = hasDownload;
        NotesButton.IsEnabled = !string.IsNullOrWhiteSpace(result.Manifest?.ReleaseNotesUrl);
    }

    private void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastResult?.Manifest?.DownloadUrl == null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(_lastResult.Manifest.DownloadUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open download link:\n{ex.Message}", "Updates", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void NotesButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastResult?.Manifest?.ReleaseNotesUrl == null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(_lastResult.Manifest.ReleaseNotesUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open release notes:\n{ex.Message}", "Updates", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

