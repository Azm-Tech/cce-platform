using CCE.Domain.Common;

namespace CCE.Domain.PlatformSettings;

public sealed class KnowledgePartner : AggregateRoot<System.Guid>
{
    private KnowledgePartner(
        System.Guid id,
        System.Guid aboutSettingsId,
        string nameAr,
        string nameEn,
        string? logoUrl,
        string? websiteUrl,
        string? descriptionAr,
        string? descriptionEn,
        int orderIndex) : base(id)
    {
        AboutSettingsId = aboutSettingsId;
        NameAr = nameAr;
        NameEn = nameEn;
        LogoUrl = logoUrl;
        WebsiteUrl = websiteUrl;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        OrderIndex = orderIndex;
    }

    public System.Guid AboutSettingsId { get; private set; }
    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public string? DescriptionAr { get; private set; }
    public string? DescriptionEn { get; private set; }
    public int OrderIndex { get; private set; }

    public static KnowledgePartner Create(
        System.Guid aboutSettingsId,
        string nameAr,
        string nameEn,
        string? logoUrl,
        string? websiteUrl,
        string? descriptionAr,
        string? descriptionEn,
        int orderIndex = 0)
    {
        if (aboutSettingsId == System.Guid.Empty)
            throw new DomainException("AboutSettingsId is required.");
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn))
            throw new DomainException("NameEn is required.");
        return new KnowledgePartner(
            System.Guid.NewGuid(), aboutSettingsId,
            nameAr, nameEn, logoUrl, websiteUrl,
            descriptionAr, descriptionEn, orderIndex);
    }

    public void UpdateContent(
        string nameAr,
        string nameEn,
        string? logoUrl,
        string? websiteUrl,
        string? descriptionAr,
        string? descriptionEn)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn))
            throw new DomainException("NameEn is required.");
        NameAr = nameAr;
        NameEn = nameEn;
        LogoUrl = logoUrl;
        WebsiteUrl = websiteUrl;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
    }

    public void Reorder(int orderIndex) => OrderIndex = orderIndex;
}
