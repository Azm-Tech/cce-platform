namespace CCE.Domain.Content;

/// <summary>
/// ClamAV result for an <c>AssetFile</c>. Files with status other than
/// <see cref="Clean"/> are blocked from public download/render.
/// </summary>
public enum VirusScanStatus
{
    /// <summary>Upload succeeded but ClamAV hasn't scanned yet.</summary>
    Pending = 0,

    /// <summary>ClamAV scanned with no detection.</summary>
    Clean = 1,

    /// <summary>ClamAV detected a signature; the asset is quarantined.</summary>
    Infected = 2,

    /// <summary>Scan failed (ClamAV error / timeout); manual review required.</summary>
    ScanFailed = 3,
}
