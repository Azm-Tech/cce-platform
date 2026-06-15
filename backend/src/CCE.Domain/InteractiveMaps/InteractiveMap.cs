using System.Text.RegularExpressions;
using CCE.Domain.Common;

namespace CCE.Domain.InteractiveMaps;

[Audited]
public sealed class InteractiveMap : Entity<System.Guid>
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private InteractiveMap(
        System.Guid id,
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn,
        string slug) : base(id)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        Slug = slug;
        IsActive = true;
    }

    public string NameAr { get; private set; }

    public string NameEn { get; private set; }

    public string? DescriptionAr { get; private set; }

    public string? DescriptionEn { get; private set; }

    public string Slug { get; private set; }

    public bool IsActive { get; private set; }

    public static InteractiveMap Create(
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn,
        string slug)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn))
            throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug))
            throw new DomainException($"Slug '{slug}' must be kebab-case (a-z, 0-9, hyphens).");

        return new InteractiveMap(
            id: System.Guid.NewGuid(),
            nameAr: nameAr,
            nameEn: nameEn,
            descriptionAr: descriptionAr,
            descriptionEn: descriptionEn,
            slug: slug);
    }

    public void UpdateDetails(
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn,
        string slug)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn))
            throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug))
            throw new DomainException($"Slug '{slug}' must be kebab-case (a-z, 0-9, hyphens).");

        NameAr = nameAr;
        NameEn = nameEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        Slug = slug;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
