using System.Text.RegularExpressions;
using CCE.Domain.Common;

namespace CCE.Domain.Community;

[Audited]
public sealed class Topic : SoftDeletableEntity<System.Guid>
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private Topic(
        System.Guid id, string nameAr, string nameEn,
        string descriptionAr, string descriptionEn,
        string slug, System.Guid? parentId,
        string? iconUrl, int orderIndex) : base(id)
    {
        NameAr = nameAr; NameEn = nameEn;
        DescriptionAr = descriptionAr; DescriptionEn = descriptionEn;
        Slug = slug; ParentId = parentId;
        IconUrl = iconUrl; OrderIndex = orderIndex;
        IsActive = true;
    }

    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public string DescriptionAr { get; private set; }
    public string DescriptionEn { get; private set; }
    public string Slug { get; private set; }
    public System.Guid? ParentId { get; private set; }
    public string? IconUrl { get; private set; }
    public int OrderIndex { get; private set; }
    public bool IsActive { get; private set; }

    public static Topic Create(
        string nameAr, string nameEn,
        string descriptionAr, string descriptionEn,
        string slug, System.Guid? parentId,
        string? iconUrl, int orderIndex)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug))
        {
            throw new DomainException($"slug '{slug}' must be kebab-case.");
        }
        if (iconUrl is not null
            && !iconUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("IconUrl must use https://.");
        }
        return new Topic(System.Guid.NewGuid(), nameAr, nameEn,
            descriptionAr, descriptionEn, slug, parentId, iconUrl, orderIndex);
    }

    public void UpdateContent(string nameAr, string nameEn, string descriptionAr, string descriptionEn)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        NameAr = nameAr; NameEn = nameEn;
        DescriptionAr = descriptionAr; DescriptionEn = descriptionEn;
    }

    public void Reorder(int orderIndex) => OrderIndex = orderIndex;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
