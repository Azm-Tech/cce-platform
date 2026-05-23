using CCE.Domain.Common;
using CCE.Domain.PlatformSettings.ValueObjects;

namespace CCE.Domain.PlatformSettings;

[Audited]
public sealed class PoliciesSettings : AggregateRoot<System.Guid>
{
    private PoliciesSettings() : base(System.Guid.Empty) { } // EF Core materialization

    private PoliciesSettings(System.Guid id) : base(id) { }

    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();

    public System.Collections.Generic.ICollection<PolicySection> Sections { get; private set; } = [];

    public static PoliciesSettings Create(System.Guid by, ISystemClock clock)
    {
        var settings = new PoliciesSettings(System.Guid.NewGuid());
        settings.MarkAsCreated(by, clock);
        return settings;
    }

    public PolicySection AddSection(
        PolicySectionType type,
        LocalizedText title,
        LocalizedText content,
        System.Guid by,
        ISystemClock clock)
    {
        var nextOrder = Sections.Count > 0 ? Sections.Max(s => s.OrderIndex) + 1 : 0;
        var section = PolicySection.Create(Id, type, title, content, nextOrder, by, clock);
        Sections.Add(section);
        return section;
    }

    public void RemoveSection(PolicySection section)
    {
        if (!Sections.Any(s => s.Id == section.Id))
            throw new DomainException("Section not found in this PoliciesSettings.");

        Sections.Remove(section);
        ReindexSections();
    }

    public void UpdateSection(
        PolicySection section,
        LocalizedText title,
        LocalizedText content,
        System.Guid by,
        ISystemClock clock)
    {
        if (!Sections.Any(s => s.Id == section.Id))
            throw new DomainException("Section does not belong to this PoliciesSettings.");

        section.UpdateContent(title, content, by, clock);
    }

    public void ReorderSection(PolicySection section, int newOrderIndex)
    {
        if (!Sections.Any(s => s.Id == section.Id))
            throw new DomainException("Section does not belong to this PoliciesSettings.");

        section.Reorder(newOrderIndex);
        ReindexSections();
    }

    private void ReindexSections()
    {
        var ordered = Sections.OrderBy(s => s.OrderIndex).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].Reorder(i);
        }
    }
}
