using System;
using System.Collections.Generic;

namespace KvmSwitch.Core.Models;

public class UpdateManifest
{
    public string Version { get; set; } = "1.0.0";
    public string? DownloadUrl { get; set; }
    public string? ReleaseNotesUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<string> Notes { get; set; } = new();
}

