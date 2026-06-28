using CCE.Domain.Common;

namespace CCE.Domain.InteractiveMaps;

[Audited]
public sealed class InteractiveMap : Entity<System.Guid>
{
    private InteractiveMap(
        System.Guid id,
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn) : base(id)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        IsActive = true;
    }

    public string NameAr { get; private set; }

    public string NameEn { get; private set; }

    public string? DescriptionAr { get; private set; }

    public string? DescriptionEn { get; private set; }

    public bool IsActive { get; private set; }

    public static InteractiveMap Create(
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn))
            throw new DomainException("NameEn is required.");

        return new InteractiveMap(
            id: System.Guid.NewGuid(),
            nameAr: nameAr,
            nameEn: nameEn,
            descriptionAr: descriptionAr,
            descriptionEn: descriptionEn);
    }

    public void UpdateDetails(
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn))
            throw new DomainException("NameEn is required.");

        NameAr = nameAr;
        NameEn = nameEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
