using CCE.Domain.Common;

namespace CCE.Domain.Identity;

public sealed class InterestTopic : Entity<System.Guid>
{
    private InterestTopic(System.Guid id, string nameAr, string nameEn, string category) : base(id)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        Category = category;
        IsActive = true;
    }

    public string NameAr { get; private set; }

    public string NameEn { get; private set; }

    public string Category { get; private set; }

    public bool IsActive { get; private set; }

    public static InterestTopic Create(string nameAr, string nameEn, string category)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn))
            throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(category))
            throw new DomainException("Category is required.");

        return new InterestTopic(System.Guid.NewGuid(), nameAr.Trim(), nameEn.Trim(), category.Trim());
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
