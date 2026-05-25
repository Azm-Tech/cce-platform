using CCE.Domain.Common;
using CCE.Domain.PlatformSettings.ValueObjects;

namespace CCE.Domain.PlatformSettings;

public sealed class GlossaryEntry : AuditableEntity<System.Guid>
{
    private GlossaryEntry() : base(System.Guid.Empty) { } // EF Core materialization

    private GlossaryEntry(
        System.Guid id,
        System.Guid aboutSettingsId,
        LocalizedText term,
        LocalizedText definition,
        int orderIndex) : base(id)
    {
        AboutSettingsId = aboutSettingsId;
        Term = term;
        Definition = definition;
        OrderIndex = orderIndex;
    }

    public System.Guid AboutSettingsId { get; private set; }
    public LocalizedText Term { get; private set; } = null!;
    public LocalizedText Definition { get; private set; } = null!;
    public int OrderIndex { get; private set; }

    public static GlossaryEntry Create(
        System.Guid aboutSettingsId,
        LocalizedText term,
        LocalizedText definition,
        int orderIndex,
        System.Guid by,
        ISystemClock clock)
    {
        if (aboutSettingsId == System.Guid.Empty)
            throw new DomainException("AboutSettingsId is required.");

        var entry = new GlossaryEntry(
            System.Guid.NewGuid(), aboutSettingsId,
            term, definition, orderIndex);
        entry.MarkAsCreated(by, clock);
        return entry;
    }

    public void UpdateContent(
        LocalizedText term,
        LocalizedText definition,
        System.Guid by,
        ISystemClock clock)
    {
        Term = term;
        Definition = definition;
        MarkAsModified(by, clock);
    }

    public void Reorder(int orderIndex) => OrderIndex = orderIndex;
}
