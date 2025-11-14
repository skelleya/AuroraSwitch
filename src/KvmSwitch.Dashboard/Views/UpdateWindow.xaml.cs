using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using KvmSwitch.Core.Interfaces;

namespace KvmSwitch.Dashboard.Views;

public partial class UpdateWindow : Window
{
    private readonly IUpdateService _updateService;
    private readonly bool _showEvenIfCurrent;
    private UpdateCheckResult? _lastResult;
    private bool _isChecking;
    private bool _isDownloading;

    private enum PrimaryAction
    {
        CheckAgain,
        Download
    }

    private PrimaryAction _primaryAction = PrimaryAction.CheckAgain;

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
        if (_isChecking || _isDownloading)
        {
            return;
        }

        _isChecking = true;
        DownloadProgress.Visibility = System.Windows.Visibility.Collapsed;
        DownloadProgress.IsIndeterminate = false;
        DownloadProgress.Value = 0;
        DownloadButton.IsEnabled = false;
        DownloadButton.Content = "Checking...";
        StatusText.Text = "Checking for updates...";

        var result = await _updateService.CheckForUpdatesAsync();
        _lastResult = result;

        CurrentVersionText.Text = result.CurrentVersion.ToString();
        LatestVersionText.Text = result.LatestVersion.ToString();

        if (!result.IsSuccess)
        {
            StatusText.Text = $"Update check failed: {result.ErrorMessage}";
            DownloadButton.Content = "Check Again";
            DownloadButton.IsEnabled = true;
            _primaryAction = PrimaryAction.CheckAgain;
            _isChecking = false;
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
        NotesButton.IsEnabled = !string.IsNullOrWhiteSpace(result.Manifest?.ReleaseNotesUrl);

        if (result.IsUpdateAvailable && hasDownload)
        {
            DownloadButton.Content = "Download Update";
            DownloadButton.IsEnabled = true;
            _primaryAction = PrimaryAction.Download;
        }
        else
        {
            DownloadButton.Content = "Check Again";
            DownloadButton.IsEnabled = true;
            _primaryAction = PrimaryAction.CheckAgain;
        }

        _isChecking = false;
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_primaryAction == PrimaryAction.CheckAgain)
        {
            await CheckAsync();
            return;
        }

        await DownloadUpdateAsync();
    }

    private async Task DownloadUpdateAsync()
    {
        if (_lastResult?.Manifest?.DownloadUrl == null || _isDownloading)
        {
            return;
        }

        if (!Uri.TryCreate(_lastResult.Manifest.DownloadUrl, UriKind.Absolute, out var uri))
        {
            MessageBox.Show("Download URL is invalid.", "Updates", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _isDownloading = true;
        DownloadButton.IsEnabled = false;
        NotesButton.IsEnabled = false;
        DownloadProgress.Visibility = Visibility.Visible;
        DownloadProgress.IsIndeterminate = true;
        StatusText.Text = "Preparing download...";

        var tempDir = Path.Combine(Path.GetTempPath(), "AuroraSwitch", "Updates");
        Directory.CreateDirectory(tempDir);

        var fileName = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "AuroraSwitchSetup.exe";
        }

        var destinationPath = Path.Combine(tempDir, fileName);

        try
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
            if (totalBytes.HasValue)
            {
                DownloadProgress.IsIndeterminate = false;
                DownloadProgress.Minimum = 0;
                DownloadProgress.Maximum = totalBytes.Value;
                DownloadProgress.Value = 0;
            }

            await using var downloadStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = File.Create(destinationPath);

            var buffer = new byte[81920];
            long bytesRead = 0;
            int read;
            while ((read = await downloadStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read));
                bytesRead += read;

                if (totalBytes.HasValue)
                {
                    DownloadProgress.Value = bytesRead;
                    var percent = bytesRead * 100d / totalBytes.Value;
                    StatusText.Text = $"Downloading update... {percent:0}%";
                }
                else
                {
                    StatusText.Text = $"Downloading update... {FormatBytes(bytesRead)}";
                }
            }

            DownloadProgress.Visibility = Visibility.Collapsed;
            StatusText.Text = "Download complete. Launching installer...";

            Process.Start(new ProcessStartInfo(destinationPath) { UseShellExecute = true });
            Close();
        }
        catch (Exception ex)
        {
            DownloadProgress.Visibility = Visibility.Collapsed;
            StatusText.Text = "Download failed.";
            MessageBox.Show($"Failed to download update:\n{ex.Message}", "Updates", MessageBoxButton.OK, MessageBoxImage.Error);
            DownloadButton.IsEnabled = true;
            NotesButton.IsEnabled = _lastResult?.Manifest?.ReleaseNotesUrl != null;
        }
        finally
        {
            _isDownloading = false;
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

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}

