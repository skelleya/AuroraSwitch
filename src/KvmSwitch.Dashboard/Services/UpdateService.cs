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
    private const string DefaultManifestUrl = "https://raw.githubusercontent.com/skelleya/AuroraSwitch/master/docs/update-manifest.json";

    private readonly HttpClient _httpClient;
    private readonly string? _manifestUrl;
    private readonly string _repo;

    public Version CurrentVersion { get; }

    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AuroraSwitchDashboard/1.0");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

        var explicitManifest = Environment.GetEnvironmentVariable("AURORASWITCH_UPDATE_MANIFEST");
        _manifestUrl = string.IsNullOrWhiteSpace(explicitManifest) ? null : explicitManifest.Trim();
        _repo = Environment.GetEnvironmentVariable("AURORASWITCH_GITHUB_REPO")
            ?? "skelleya/AuroraSwitch";

        CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        UpdateManifest? manifest = null;

        if (!string.IsNullOrWhiteSpace(_manifestUrl))
        {
            manifest = await FetchManifestFromUrlAsync(_manifestUrl!, errors, cancellationToken);
        }
        else
        {
            manifest = await FetchLatestReleaseAsync(errors, cancellationToken);
            if (manifest == null)
            {
                manifest = await FetchManifestFromUrlAsync(DefaultManifestUrl, errors, cancellationToken);
            }
        }

        if (manifest == null)
        {
            var errorMessage = errors.Count > 0
                ? string.Join(" | ", errors)
                : "Manifest could not be parsed.";
            _logger.LogWarning("Update check failed: {Message}", errorMessage);
            return UpdateCheckResult.Failure(errorMessage, CurrentVersion);
        }

        if (!Version.TryParse(manifest.Version, out var latestVersion))
        {
            errors.Add($"Invalid version string '{manifest.Version}'.");
            _logger.LogWarning("Manifest version {Version} is invalid.", manifest.Version);
            latestVersion = CurrentVersion;
        }

        var isNewer = latestVersion > CurrentVersion;
        return UpdateCheckResult.Success(isNewer, CurrentVersion, latestVersion, manifest);
    }

    private async Task<UpdateManifest?> FetchManifestFromUrlAsync(string url, List<string> errors, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Checking for updates via manifest {Url}", url);
            return await _httpClient.GetFromJsonAsync<UpdateManifest>(url, cancellationToken);
        }
        catch (Exception ex)
        {
            errors.Add($"Manifest fetch failed ({url}): {ex.Message}");
            _logger.LogWarning(ex, "Manifest fetch failed for {Url}", url);
            return null;
        }
    }

    private async Task<UpdateManifest?> FetchLatestReleaseAsync(List<string> errors, CancellationToken cancellationToken)
    {
        var url = $"https://api.github.com/repos/{_repo}/releases/latest";
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var message = $"GitHub API returned {(int)response.StatusCode}";
                errors.Add(message);
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
        catch (Exception ex)
        {
            errors.Add($"GitHub release fetch failed: {ex.Message}");
            _logger.LogWarning(ex, "GitHub release fetch failed");
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}


