using System;
using System.Threading;
using System.Threading.Tasks;
using KvmSwitch.Core.Models;

namespace KvmSwitch.Core.Interfaces;

public interface IUpdateService
{
    Version CurrentVersion { get; }
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}

public sealed record UpdateCheckResult(
    bool IsSuccess,
    bool IsUpdateAvailable,
    Version CurrentVersion,
    Version LatestVersion,
    UpdateManifest? Manifest,
    string? ErrorMessage)
{
    public static UpdateCheckResult Success(bool isUpdateAvailable, Version current, Version latest, UpdateManifest? manifest) =>
        new(true, isUpdateAvailable, current, latest, manifest, null);

    public static UpdateCheckResult Failure(string error, Version current) =>
        new(false, false, current, current, null, error);
}

