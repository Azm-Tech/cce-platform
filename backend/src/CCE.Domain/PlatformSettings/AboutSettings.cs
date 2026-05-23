using CCE.Domain.Common;
using CCE.Domain.PlatformSettings.ValueObjects;

namespace CCE.Domain.PlatformSettings;

[Audited]
public sealed class AboutSettings : AggregateRoot<System.Guid>
{
    private AboutSettings() : base(System.Guid.Empty) { } // EF Core materialization

    private AboutSettings(System.Guid id, LocalizedText description) : base(id)
    {
        Description = description;
    }

    public LocalizedText Description { get; private set; } = null!;
    public string? HowToUseVideoUrl { get; private set; }
    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();

    public System.Collections.Generic.ICollection<GlossaryEntry> GlossaryEntries { get; private set; } = [];
    public System.Collections.Generic.ICollection<KnowledgePartner> KnowledgePartners { get; private set; } = [];

    public static AboutSettings Create(LocalizedText description, System.Guid by, ISystemClock clock)
    {
        var settings = new AboutSettings(System.Guid.NewGuid(), description);
        settings.MarkAsCreated(by, clock);
        return settings;
    }

    public void UpdateContent(LocalizedText description, string? howToUseVideoUrl, System.Guid by, ISystemClock clock)
    {
        Description = description;
        HowToUseVideoUrl = howToUseVideoUrl;
        MarkAsModified(by, clock);
    }

    public GlossaryEntry AddGlossaryEntry(LocalizedText term, LocalizedText definition, System.Guid by, ISystemClock clock)
    {
        var nextOrder = GlossaryEntries.Count > 0 ? GlossaryEntries.Max(e => e.OrderIndex) + 1 : 0;
        var entry = GlossaryEntry.Create(Id, term, definition, nextOrder, by, clock);
        GlossaryEntries.Add(entry);
        return entry;
    }

    public void RemoveGlossaryEntry(GlossaryEntry entry)
    {
        if (!GlossaryEntries.Any(e => e.Id == entry.Id))
            throw new DomainException("Glossary entry not found in this AboutSettings.");

        GlossaryEntries.Remove(entry);
        ReindexGlossary();
    }

    public void UpdateGlossaryEntry(GlossaryEntry entry, LocalizedText term, LocalizedText definition, System.Guid by, ISystemClock clock)
    {
        if (!GlossaryEntries.Any(e => e.Id == entry.Id))
            throw new DomainException("Glossary entry does not belong to this AboutSettings.");

        entry.UpdateContent(term, definition, by, clock);
    }

    public KnowledgePartner AddKnowledgePartner(
        LocalizedText name,
        LocalizedText? description,
        string? logoUrl,
        string? websiteUrl,
        System.Guid by,
        ISystemClock clock)
    {
        var nextOrder = KnowledgePartners.Count > 0 ? KnowledgePartners.Max(p => p.OrderIndex) + 1 : 0;
        var partner = KnowledgePartner.Create(Id, name, description, logoUrl, websiteUrl, nextOrder, by, clock);
        KnowledgePartners.Add(partner);
        return partner;
    }

    public void RemoveKnowledgePartner(KnowledgePartner partner)
    {
        if (!KnowledgePartners.Any(p => p.Id == partner.Id))
            throw new DomainException("Knowledge partner not found in this AboutSettings.");

        KnowledgePartners.Remove(partner);
        ReindexPartners();
    }

    public void UpdateKnowledgePartner(
        KnowledgePartner partner,
        LocalizedText name,
        LocalizedText? description,
        string? logoUrl,
        string? websiteUrl,
        System.Guid by,
        ISystemClock clock)
    {
        if (!KnowledgePartners.Any(p => p.Id == partner.Id))
            throw new DomainException("Knowledge partner does not belong to this AboutSettings.");

        partner.UpdateContent(name, description, logoUrl, websiteUrl, by, clock);
    }

    private void ReindexGlossary()
    {
        var ordered = GlossaryEntries.OrderBy(e => e.OrderIndex).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].Reorder(i);
        }
    }

    private void ReindexPartners()
    {
        var ordered = KnowledgePartners.OrderBy(p => p.OrderIndex).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].Reorder(i);
        }
    }
}
