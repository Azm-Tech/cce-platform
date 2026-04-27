using CCE.Domain.Common;

namespace CCE.Domain.Content;

/// <summary>
/// File-storage handle. Carries the URL/key, mime type, size, and ClamAV result.
/// State transitions: <see cref="VirusScanStatus.Pending"/> → exactly one of
/// <see cref="VirusScanStatus.Clean"/>, <see cref="VirusScanStatus.Infected"/>,
/// <see cref="VirusScanStatus.ScanFailed"/>. Once a terminal status is set, it cannot
/// change — re-scan requires a new asset.
/// </summary>
[Audited]
public sealed class AssetFile : Entity<System.Guid>
{
    private AssetFile(
        System.Guid id,
        string url,
        string originalFileName,
        long sizeBytes,
        string mimeType,
        System.Guid uploadedById,
        System.DateTimeOffset uploadedOn) : base(id)
    {
        Url = url;
        OriginalFileName = originalFileName;
        SizeBytes = sizeBytes;
        MimeType = mimeType;
        UploadedById = uploadedById;
        UploadedOn = uploadedOn;
        VirusScanStatus = VirusScanStatus.Pending;
    }

    public string Url { get; private set; }

    public string OriginalFileName { get; private set; }

    public long SizeBytes { get; private set; }

    public string MimeType { get; private set; }

    public System.Guid UploadedById { get; private set; }

    public System.DateTimeOffset UploadedOn { get; private set; }

    public VirusScanStatus VirusScanStatus { get; private set; }

    public System.DateTimeOffset? ScannedOn { get; private set; }

    public static AssetFile Register(
        string url,
        string originalFileName,
        long sizeBytes,
        string mimeType,
        System.Guid uploadedById,
        ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainException("Url is required.");
        }
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new DomainException("OriginalFileName is required.");
        }
        if (sizeBytes <= 0)
        {
            throw new DomainException("SizeBytes must be positive.");
        }
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new DomainException("MimeType is required.");
        }
        if (uploadedById == System.Guid.Empty)
        {
            throw new DomainException("UploadedById is required.");
        }
        return new AssetFile(
            id: System.Guid.NewGuid(),
            url: url,
            originalFileName: originalFileName,
            sizeBytes: sizeBytes,
            mimeType: mimeType,
            uploadedById: uploadedById,
            uploadedOn: clock.UtcNow);
    }

    public void MarkClean(ISystemClock clock) => Transition(VirusScanStatus.Clean, clock);

    public void MarkInfected(ISystemClock clock) => Transition(VirusScanStatus.Infected, clock);

    public void MarkScanFailed(ISystemClock clock) => Transition(VirusScanStatus.ScanFailed, clock);

    private void Transition(VirusScanStatus terminal, ISystemClock clock)
    {
        if (VirusScanStatus != VirusScanStatus.Pending)
        {
            throw new DomainException($"Cannot mark a {VirusScanStatus} asset — must be Pending.");
        }
        VirusScanStatus = terminal;
        ScannedOn = clock.UtcNow;
    }
}
