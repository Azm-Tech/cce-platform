using CCE.Domain.Common;
using CCE.Domain.PlatformSettings.ValueObjects;

namespace CCE.Domain.PlatformSettings;

public sealed class KnowledgePartner : AuditableEntity<System.Guid>
{
    private KnowledgePartner() : base(System.Guid.Empty) { } // EF Core materialization

    private KnowledgePartner(
        System.Guid id,
        System.Guid aboutSettingsId,
        LocalizedText name,
        LocalizedText? description,
        string? logoUrl,
        string? websiteUrl,
        int orderIndex) : base(id)
    {
        AboutSettingsId = aboutSettingsId;
        Name = name;
        Description = description;
        LogoUrl = logoUrl;
        WebsiteUrl = websiteUrl;
        OrderIndex = orderIndex;
    }

    public System.Guid AboutSettingsId { get; private set; }
    public LocalizedText Name { get; private set; } = null!;
    public LocalizedText? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public int OrderIndex { get; private set; }

    public static KnowledgePartner Create(
        System.Guid aboutSettingsId,
        LocalizedText name,
        LocalizedText? description,
        string? logoUrl,
        string? websiteUrl,
        int orderIndex,
        System.Guid by,
        ISystemClock clock)
    {
        if (aboutSettingsId == System.Guid.Empty)
            throw new DomainException("AboutSettingsId is required.");

        var partner = new KnowledgePartner(
            System.Guid.NewGuid(), aboutSettingsId,
            name, description, logoUrl, websiteUrl, orderIndex);
        partner.MarkAsCreated(by, clock);
        return partner;
    }

    public void UpdateContent(
        LocalizedText name,
        LocalizedText? description,
        string? logoUrl,
        string? websiteUrl,
        System.Guid by,
        ISystemClock clock)
    {
        Name = name;
        Description = description;
        LogoUrl = logoUrl;
        WebsiteUrl = websiteUrl;
        MarkAsModified(by, clock);
    }

    public void Reorder(int orderIndex) => OrderIndex = orderIndex;
}
