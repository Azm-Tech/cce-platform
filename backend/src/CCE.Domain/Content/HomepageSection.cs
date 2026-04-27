using CCE.Domain.Common;

namespace CCE.Domain.Content;

/// <summary>
/// One block on the public homepage. Admins reorder via <see cref="OrderIndex"/>; the
/// rendering layer queries <c>WHERE IsActive = true ORDER BY OrderIndex</c>.
/// </summary>
[Audited]
public sealed class HomepageSection : Entity<System.Guid>, ISoftDeletable
{
    private HomepageSection(
        System.Guid id,
        HomepageSectionType sectionType,
        int orderIndex,
        string contentAr,
        string contentEn) : base(id)
    {
        SectionType = sectionType;
        OrderIndex = orderIndex;
        ContentAr = contentAr;
        ContentEn = contentEn;
        IsActive = true;
    }

    public HomepageSectionType SectionType { get; private set; }
    public int OrderIndex { get; private set; }
    public string ContentAr { get; private set; }
    public string ContentEn { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public System.DateTimeOffset? DeletedOn { get; private set; }
    public System.Guid? DeletedById { get; private set; }

    public static HomepageSection Create(HomepageSectionType type, int orderIndex, string contentAr, string contentEn)
    {
        return new HomepageSection(System.Guid.NewGuid(), type, orderIndex,
            contentAr ?? string.Empty, contentEn ?? string.Empty);
    }

    public void Reorder(int orderIndex) => OrderIndex = orderIndex;

    public void UpdateContent(string contentAr, string contentEn)
    {
        ContentAr = contentAr ?? string.Empty;
        ContentEn = contentEn ?? string.Empty;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void SoftDelete(System.Guid deletedById, ISystemClock clock)
    {
        if (deletedById == System.Guid.Empty) throw new DomainException("DeletedById is required.");
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedById = deletedById;
        DeletedOn = clock.UtcNow;
    }
}
