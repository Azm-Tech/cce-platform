using System.Text.RegularExpressions;
using CCE.Domain.Common;

namespace CCE.Domain.KnowledgeMaps;

[Audited]
public sealed class KnowledgeMap : SoftDeletableAggregateRoot<System.Guid>
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private KnowledgeMap(System.Guid id, string nameAr, string nameEn,
        string descriptionAr, string descriptionEn, string slug) : base(id)
    {
        NameAr = nameAr; NameEn = nameEn;
        DescriptionAr = descriptionAr; DescriptionEn = descriptionEn;
        Slug = slug; IsActive = true;
    }

    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public string DescriptionAr { get; private set; }
    public string DescriptionEn { get; private set; }
    public string Slug { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();

    public static KnowledgeMap Create(string nameAr, string nameEn,
        string descriptionAr, string descriptionEn, string slug)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug))
            throw new DomainException($"slug '{slug}' must be kebab-case.");
        return new KnowledgeMap(System.Guid.NewGuid(), nameAr, nameEn, descriptionAr, descriptionEn, slug);
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

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
