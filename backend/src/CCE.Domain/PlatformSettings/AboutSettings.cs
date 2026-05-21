using CCE.Domain.Common;

namespace CCE.Domain.PlatformSettings;

[Audited]
public sealed class AboutSettings : AggregateRoot<System.Guid>
{
    private AboutSettings(
        System.Guid id,
        string descriptionAr,
        string descriptionEn) : base(id)
    {
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
    }

    public string DescriptionAr { get; private set; }
    public string DescriptionEn { get; private set; }
    public string? HowToUseVideoUrl { get; private set; }
    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();

    public static AboutSettings Create(string descriptionAr, string descriptionEn)
    {
        if (string.IsNullOrWhiteSpace(descriptionAr))
            throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn))
            throw new DomainException("DescriptionEn is required.");
        return new AboutSettings(System.Guid.NewGuid(), descriptionAr, descriptionEn);
    }

    public void UpdateContent(
        string descriptionAr,
        string descriptionEn,
        string? howToUseVideoUrl)
    {
        if (string.IsNullOrWhiteSpace(descriptionAr))
            throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn))
            throw new DomainException("DescriptionEn is required.");
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        HowToUseVideoUrl = howToUseVideoUrl;
    }
}
