using System.Net.Http;
using System.Net.Http.Json;
using KvmSwitch.Core.Models;

const string defaultManifest = "https://raw.githubusercontent.com/your-org/AuroraSwitch/main/docs/update-manifest.json";

var manifestUrl = defaultManifest;
var autoDownload = false;

foreach (var arg in args)
{
    if (arg.StartsWith("--manifest=", StringComparison.OrdinalIgnoreCase))
    {
        manifestUrl = arg.Split('=', 2)[1];
    }
    if (string.Equals(arg, "--download", StringComparison.OrdinalIgnoreCase))
    {
        autoDownload = true;
    }
}

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AuroraSwitchUpdater/1.0");

Console.WriteLine($"Checking updates via: {manifestUrl}");

UpdateManifest? manifest;
try
{
    manifest = await httpClient.GetFromJsonAsync<UpdateManifest>(manifestUrl);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to fetch manifest: {ex.Message}");
    return 1;
}

if (manifest == null)
{
    Console.Error.WriteLine("Manifest empty.");
    return 1;
}

Console.WriteLine($"Latest Version: {manifest.Version}");
Console.WriteLine($"Download Url: {manifest.DownloadUrl}");
Console.WriteLine($"Release Notes: {manifest.ReleaseNotesUrl}");

if (manifest.Notes.Count > 0)
{
    Console.WriteLine("Notes:");
    foreach (var note in manifest.Notes)
    {
        Console.WriteLine($" - {note}");
    }
}

if (autoDownload && !string.IsNullOrWhiteSpace(manifest.DownloadUrl))
{
    var downloadUri = new Uri(manifest.DownloadUrl);
    var fileName = Path.GetFileName(downloadUri.LocalPath);
    var targetPath = Path.Combine(Path.GetTempPath(), fileName);
    Console.WriteLine($"Downloading update package to {targetPath}");

    await using var stream = await httpClient.GetStreamAsync(downloadUri);
    await using (var file = File.Create(targetPath))
    {
        await stream.CopyToAsync(file);
    }

    Console.WriteLine("Download complete. Launching installer...");
    try
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo(targetPath)
        {
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(startInfo);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to start installer: {ex.Message}");
    }
}

return 0;


