using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
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
    private readonly string _manifestUrl;

    public Version CurrentVersion { get; }

    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AuroraSwitchDashboard/1.0");

        _manifestUrl = Environment.GetEnvironmentVariable("AURORASWITCH_UPDATE_MANIFEST")
            ?? "https://raw.githubusercontent.com/skelleya/AuroraSwitch/master/docs/update-manifest.json";

        CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking for updates via {Url}", _manifestUrl);
            var manifest = await _httpClient.GetFromJsonAsync<UpdateManifest>(_manifestUrl, cancellationToken);
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

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}


