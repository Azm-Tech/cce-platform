using CCE.Domain.Common;
using CCE.Domain.PlatformSettings.ValueObjects;

namespace CCE.Domain.PlatformSettings;

public sealed class PolicySection : AuditableEntity<System.Guid>
{
    private PolicySection() : base(System.Guid.Empty) { } // EF Core materialization

    private PolicySection(
        System.Guid id,
        System.Guid policiesSettingsId,
        PolicySectionType type,
        LocalizedText title,
        LocalizedText content,
        int orderIndex) : base(id)
    {
        PoliciesSettingsId = policiesSettingsId;
        Type = type;
        Title = title;
        Content = content;
        OrderIndex = orderIndex;
    }

    public System.Guid PoliciesSettingsId { get; private set; }
    public PolicySectionType Type { get; private set; }
    public LocalizedText Title { get; private set; } = null!;
    public LocalizedText Content { get; private set; } = null!;
    public int OrderIndex { get; private set; }

    public static PolicySection Create(
        System.Guid policiesSettingsId,
        PolicySectionType type,
        LocalizedText title,
        LocalizedText content,
        int orderIndex,
        System.Guid by,
        ISystemClock clock)
    {
        if (policiesSettingsId == System.Guid.Empty)
            throw new DomainException("PoliciesSettingsId is required.");

        var section = new PolicySection(
            System.Guid.NewGuid(), policiesSettingsId,
            type, title, content, orderIndex);
        section.MarkAsCreated(by, clock);
        return section;
    }

    public void UpdateContent(
        LocalizedText title,
        LocalizedText content,
        System.Guid by,
        ISystemClock clock)
    {
        Title = title;
        Content = content;
        MarkAsModified(by, clock);
    }

    public void Reorder(int orderIndex) => OrderIndex = orderIndex;
}
