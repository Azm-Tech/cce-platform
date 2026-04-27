using System.Text.RegularExpressions;
using CCE.Domain.Common;

namespace CCE.Domain.Content;

/// <summary>
/// Hierarchical taxonomy node for <c>Resource</c>. Self-referencing via
/// <see cref="ParentId"/>. Root categories have null parent. Slugs are kebab-case
/// (lowercase a-z, digits, hyphens). Inactive categories are hidden from public UI
/// but their resources remain accessible by direct link.
/// </summary>
[Audited]
public sealed class ResourceCategory : Entity<System.Guid>
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private ResourceCategory(
        System.Guid id,
        string nameAr,
        string nameEn,
        string slug,
        System.Guid? parentId,
        int orderIndex) : base(id)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        Slug = slug;
        ParentId = parentId;
        OrderIndex = orderIndex;
        IsActive = true;
    }

    public string NameAr { get; private set; }

    public string NameEn { get; private set; }

    public string Slug { get; private set; }

    public System.Guid? ParentId { get; private set; }

    public int OrderIndex { get; private set; }

    public bool IsActive { get; private set; }

    public static ResourceCategory Create(
        string nameAr,
        string nameEn,
        string slug,
        System.Guid? parentId,
        int orderIndex)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
        {
            throw new DomainException("NameAr is required.");
        }
        if (string.IsNullOrWhiteSpace(nameEn))
        {
            throw new DomainException("NameEn is required.");
        }
        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug))
        {
            throw new DomainException($"slug '{slug}' must be kebab-case (a-z, 0-9, hyphens).");
        }
        return new ResourceCategory(
            id: System.Guid.NewGuid(),
            nameAr: nameAr,
            nameEn: nameEn,
            slug: slug,
            parentId: parentId,
            orderIndex: orderIndex);
    }

    public void UpdateNames(string nameAr, string nameEn)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
        {
            throw new DomainException("NameAr is required.");
        }
        if (string.IsNullOrWhiteSpace(nameEn))
        {
            throw new DomainException("NameEn is required.");
        }
        NameAr = nameAr;
        NameEn = nameEn;
    }

    public void Reorder(int orderIndex) => OrderIndex = orderIndex;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
