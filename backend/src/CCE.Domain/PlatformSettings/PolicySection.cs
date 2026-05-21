using CCE.Domain.Common;

namespace CCE.Domain.PlatformSettings;

public sealed class PolicySection : AggregateRoot<System.Guid>
{
    private PolicySection(
        System.Guid id,
        System.Guid policiesSettingsId,
        PolicySectionType type,
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn,
        int orderIndex) : base(id)
    {
        PoliciesSettingsId = policiesSettingsId;
        Type = type;
        TitleAr = titleAr;
        TitleEn = titleEn;
        ContentAr = contentAr;
        ContentEn = contentEn;
        OrderIndex = orderIndex;
    }

    public System.Guid PoliciesSettingsId { get; private set; }
    public PolicySectionType Type { get; private set; }
    public string TitleAr { get; private set; }
    public string TitleEn { get; private set; }
    public string ContentAr { get; private set; }
    public string ContentEn { get; private set; }
    public int OrderIndex { get; private set; }

    public static PolicySection Create(
        System.Guid policiesSettingsId,
        PolicySectionType type,
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn,
        int orderIndex = 0)
    {
        if (policiesSettingsId == System.Guid.Empty)
            throw new DomainException("PoliciesSettingsId is required.");
        if (string.IsNullOrWhiteSpace(titleAr))
            throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn))
            throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(contentAr))
            throw new DomainException("ContentAr is required.");
        if (string.IsNullOrWhiteSpace(contentEn))
            throw new DomainException("ContentEn is required.");
        return new PolicySection(
            System.Guid.NewGuid(), policiesSettingsId,
            type, titleAr, titleEn, contentAr, contentEn, orderIndex);
    }

    public void UpdateContent(
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn)
    {
        if (string.IsNullOrWhiteSpace(titleAr))
            throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn))
            throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(contentAr))
            throw new DomainException("ContentAr is required.");
        if (string.IsNullOrWhiteSpace(contentEn))
            throw new DomainException("ContentEn is required.");
        TitleAr = titleAr;
        TitleEn = titleEn;
        ContentAr = contentAr;
        ContentEn = contentEn;
    }

    public void Reorder(int orderIndex) => OrderIndex = orderIndex;
}
