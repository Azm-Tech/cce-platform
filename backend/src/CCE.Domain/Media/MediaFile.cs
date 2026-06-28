using CCE.Domain.Common;

namespace CCE.Domain.Media;

[Audited]
public sealed class MediaFile : Entity<System.Guid>
{
    private MediaFile(
        System.Guid id,
        string storageKey,
        string url,
        string originalFileName,
        string mimeType,
        long sizeBytes,
        string? titleAr,
        string? titleEn,
        string? descriptionAr,
        string? descriptionEn,
        string? altTextAr,
        string? altTextEn,
        System.Guid uploadedById,
        System.DateTimeOffset uploadedOn) : base(id)
    {
        StorageKey = storageKey;
        Url = url;
        OriginalFileName = originalFileName;
        MimeType = mimeType;
        SizeBytes = sizeBytes;
        TitleAr = titleAr;
        TitleEn = titleEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        AltTextAr = altTextAr;
        AltTextEn = altTextEn;
        UploadedById = uploadedById;
        UploadedOn = uploadedOn;
    }

    public string StorageKey { get; private set; }
    public string Url { get; private set; }
    public string OriginalFileName { get; private set; }
    public string MimeType { get; private set; }
    public long SizeBytes { get; private set; }
    public string? TitleAr { get; private set; }
    public string? TitleEn { get; private set; }
    public string? DescriptionAr { get; private set; }
    public string? DescriptionEn { get; private set; }
    public string? AltTextAr { get; private set; }
    public string? AltTextEn { get; private set; }
    public System.Guid UploadedById { get; private set; }
    public System.DateTimeOffset UploadedOn { get; private set; }

    public static MediaFile Create(
        string storageKey,
        string url,
        string originalFileName,
        string mimeType,
        long sizeBytes,
        System.Guid uploadedById,
        ISystemClock clock,
        string? titleAr = null,
        string? titleEn = null,
        string? descriptionAr = null,
        string? descriptionEn = null,
        string? altTextAr = null,
        string? altTextEn = null)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new DomainException("StorageKey is required.");
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("Url is required.");
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new DomainException("OriginalFileName is required.");
        if (string.IsNullOrWhiteSpace(mimeType))
            throw new DomainException("MimeType is required.");
        if (sizeBytes <= 0)
            throw new DomainException("SizeBytes must be positive.");
        if (uploadedById == System.Guid.Empty)
            throw new DomainException("UploadedById is required.");

        return new MediaFile(
            System.Guid.NewGuid(),
            storageKey,
            url,
            originalFileName,
            mimeType,
            sizeBytes,
            titleAr,
            titleEn,
            descriptionAr,
            descriptionEn,
            altTextAr,
            altTextEn,
            uploadedById,
            clock.UtcNow);
    }

    public void UpdateMetadata(
        string? titleAr = null,
        string? titleEn = null,
        string? descriptionAr = null,
        string? descriptionEn = null,
        string? altTextAr = null,
        string? altTextEn = null)
    {
        if (titleAr is not null) TitleAr = titleAr;
        if (titleEn is not null) TitleEn = titleEn;
        if (descriptionAr is not null) DescriptionAr = descriptionAr;
        if (descriptionEn is not null) DescriptionEn = descriptionEn;
        if (altTextAr is not null) AltTextAr = altTextAr;
        if (altTextEn is not null) AltTextEn = altTextEn;
    }
}
