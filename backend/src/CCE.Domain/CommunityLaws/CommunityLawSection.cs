using CCE.Domain.Common;
using CCE.Domain.PlatformSettings.ValueObjects;

namespace CCE.Domain.CommunityLaws;

public sealed class CommunityLawSection : AuditableEntity<Guid>
{
    private CommunityLawSection() : base(Guid.NewGuid()) { }

    private CommunityLawSection(
        Guid id,
        LocalizedText title,
        LocalizedText content,
        int orderIndex) : base(id)
    {
        Title = title;
        Content = content;
        OrderIndex = orderIndex;
    }

    public LocalizedText Title { get; private set; } = null!;
    public LocalizedText Content { get; private set; } = null!;
    public int OrderIndex { get; private set; }

    public static CommunityLawSection Create(
        LocalizedText title,
        LocalizedText content,
        int orderIndex,
        Guid by,
        ISystemClock clock)
    {
        var section = new CommunityLawSection(
            Guid.NewGuid(), title, content, orderIndex);
        section.MarkAsCreated(by, clock);
        return section;
    }

    public void UpdateContent(
        LocalizedText title,
        LocalizedText content,
        Guid by,
        ISystemClock clock)
    {
        Title = title;
        Content = content;
        MarkAsModified(by, clock);
    }

    public void Reorder(int orderIndex) => OrderIndex = orderIndex;
}
