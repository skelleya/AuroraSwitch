using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KvmSwitch.Core.Interfaces;
using KvmSwitch.Core.Models;
using Microsoft.Extensions.Logging;

namespace KvmSwitch.Dashboard.Services;

public class UpdateService : IUpdateService, IDisposable
{
    private readonly ILogger<UpdateService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _manifestUrl;
    private readonly string _repo;

    public Version CurrentVersion { get; }

    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AuroraSwitchDashboard/1.0");

        _manifestUrl = Environment.GetEnvironmentVariable("AURORASWITCH_UPDATE_MANIFEST");
        _repo = Environment.GetEnvironmentVariable("AURORASWITCH_GITHUB_REPO")
            ?? "skelleya/AuroraSwitch";

        CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            UpdateManifest? manifest;
            if (!string.IsNullOrWhiteSpace(_manifestUrl))
            {
                _logger.LogInformation("Checking for updates via manifest {Url}", _manifestUrl);
                manifest = await _httpClient.GetFromJsonAsync<UpdateManifest>(_manifestUrl, cancellationToken);
            }
            else
            {
                _logger.LogInformation("Checking for updates via GitHub releases for repo {Repo}", _repo);
                manifest = await FetchLatestReleaseAsync(cancellationToken);
            }

            if (manifest == null)
            {
                return UpdateCheckResult.Failure("Manifest could not be parsed.", CurrentVersion);
            }

            if (!Version.TryParse(manifest.Version, out var latestVersion))
            {
                _logger.LogWarning("Manifest version {Version} is invalid.", manifest.Version);
                latestVersion = CurrentVersion;
            }

            var isNewer = latestVersion > CurrentVersion;
            return UpdateCheckResult.Success(isNewer, CurrentVersion, latestVersion, manifest);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check for updates");
            return UpdateCheckResult.Failure(ex.Message, CurrentVersion);
        }
    }

    private async Task<UpdateManifest?> FetchLatestReleaseAsync(CancellationToken cancellationToken)
    {
        var url = $"https://api.github.com/repos/{_repo}/releases/latest";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var message = $"GitHub API returned {(int)response.StatusCode}";
            _logger.LogWarning("Failed to fetch latest release: {Message}", message);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = doc.RootElement;

        var tag = root.TryGetProperty("tag_name", out var tagElement)
            ? tagElement.GetString() ?? "0.0.0"
            : "0.0.0";

        var versionText = tag.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? tag[1..] : tag;

        string? downloadUrl = null;
        if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assets.EnumerateArray())
            {
                if (!asset.TryGetProperty("browser_download_url", out var assetUrlElement))
                {
                    continue;
                }

                var assetUrl = assetUrlElement.GetString();
                if (string.IsNullOrWhiteSpace(assetUrl))
                {
                    continue;
                }

                if (asset.TryGetProperty("name", out var nameElement))
                {
                    var name = nameElement.GetString() ?? string.Empty;
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = assetUrl;
                        break;
                    }
                }

                downloadUrl ??= assetUrl;
            }
        }

        var notes = new List<string>();
        if (root.TryGetProperty("body", out var bodyElement))
        {
            var body = bodyElement.GetString();
            if (!string.IsNullOrWhiteSpace(body))
            {
                foreach (var line in body.Split('\n'))
                {
                    var trimmed = line.Trim('-').Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        notes.Add(trimmed);
                    }
                }
            }
        }

        return new UpdateManifest
        {
            Version = versionText,
            DownloadUrl = downloadUrl,
            ReleaseNotesUrl = root.TryGetProperty("html_url", out var htmlUrl) ? htmlUrl.GetString() : null,
            PublishedAt = root.TryGetProperty("published_at", out var publishedElement) && publishedElement.TryGetDateTime(out var published)
                ? published
                : (DateTime?)null,
            Notes = notes
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}


