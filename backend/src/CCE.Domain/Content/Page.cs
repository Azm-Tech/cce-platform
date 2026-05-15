using System.Text.RegularExpressions;
using CCE.Domain.Common;

namespace CCE.Domain.Content;

/// <summary>
/// Static page. Slug is unique within (<see cref="PageType"/>) — enforced by Phase 08
/// composite unique index. Content is rich-text bilingual.
/// </summary>
[Audited]
public sealed class Page : SoftDeletableAggregateRoot<System.Guid>
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private Page(
        System.Guid id,
        string slug,
        PageType pageType,
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn) : base(id)
    {
        Slug = slug;
        PageType = pageType;
        TitleAr = titleAr;
        TitleEn = titleEn;
        ContentAr = contentAr;
        ContentEn = contentEn;
    }

    public string Slug { get; private set; }
    public PageType PageType { get; private set; }
    public string TitleAr { get; private set; }
    public string TitleEn { get; private set; }
    public string ContentAr { get; private set; }
    public string ContentEn { get; private set; }
    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();

    public static Page Create(
        string slug,
        PageType pageType,
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn)
    {
        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug))
        {
            throw new DomainException($"slug '{slug}' must be kebab-case.");
        }
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(contentAr)) throw new DomainException("ContentAr is required.");
        if (string.IsNullOrWhiteSpace(contentEn)) throw new DomainException("ContentEn is required.");
        return new Page(System.Guid.NewGuid(), slug, pageType, titleAr, titleEn, contentAr, contentEn);
    }

    public void UpdateContent(string titleAr, string titleEn, string contentAr, string contentEn)
    {
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(contentAr)) throw new DomainException("ContentAr is required.");
        if (string.IsNullOrWhiteSpace(contentEn)) throw new DomainException("ContentEn is required.");
        TitleAr = titleAr;
        TitleEn = titleEn;
        ContentAr = contentAr;
        ContentEn = contentEn;
    }
}
