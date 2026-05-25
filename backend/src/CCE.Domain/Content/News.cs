using System.Text.RegularExpressions;
using CCE.Domain.Common;
using CCE.Domain.Content.Events;

namespace CCE.Domain.Content;

/// <summary>
/// News article — bilingual title + rich-text content + optional featured image.
/// Slug is unique (enforced in Phase 08 DB unique index). Soft-deletable, audited.
/// </summary>
[Audited]
public sealed class News : AggregateRoot<System.Guid>
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private News(
        System.Guid id,
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn,
        string slug,
        System.Guid authorId,
        string? featuredImageUrl) : base(id)
    {
        TitleAr = titleAr;
        TitleEn = titleEn;
        ContentAr = contentAr;
        ContentEn = contentEn;
        Slug = slug;
        AuthorId = authorId;
        FeaturedImageUrl = featuredImageUrl;
    }

    public string TitleAr { get; private set; }
    public string TitleEn { get; private set; }
    public string ContentAr { get; private set; }
    public string ContentEn { get; private set; }
    public string Slug { get; private set; }
    public System.Guid AuthorId { get; private set; }
    public string? FeaturedImageUrl { get; private set; }
    public System.DateTimeOffset? PublishedOn { get; private set; }
    public bool IsFeatured { get; private set; }
    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();

    public bool IsPublished => PublishedOn is not null;

    public static News Draft(
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn,
        string slug,
        System.Guid authorId,
        string? featuredImageUrl,
        ISystemClock clock)
    {
        _ = clock;
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(contentAr)) throw new DomainException("ContentAr is required.");
        if (string.IsNullOrWhiteSpace(contentEn)) throw new DomainException("ContentEn is required.");
        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug))
        {
            throw new DomainException($"slug '{slug}' must be kebab-case.");
        }
        if (authorId == System.Guid.Empty) throw new DomainException("AuthorId is required.");
        if (featuredImageUrl is not null
            && !featuredImageUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("FeaturedImageUrl must use https://.");
        }
        return new News(
            id: System.Guid.NewGuid(),
            titleAr: titleAr,
            titleEn: titleEn,
            contentAr: contentAr,
            contentEn: contentEn,
            slug: slug,
            authorId: authorId,
            featuredImageUrl: featuredImageUrl);
    }

    public void UpdateContent(
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn,
        string slug,
        string? featuredImageUrl)
    {
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(contentAr)) throw new DomainException("ContentAr is required.");
        if (string.IsNullOrWhiteSpace(contentEn)) throw new DomainException("ContentEn is required.");
        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug))
        {
            throw new DomainException($"slug '{slug}' must be kebab-case.");
        }
        if (featuredImageUrl is not null
            && !featuredImageUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("FeaturedImageUrl must use https://.");
        }
        TitleAr = titleAr;
        TitleEn = titleEn;
        ContentAr = contentAr;
        ContentEn = contentEn;
        Slug = slug;
        FeaturedImageUrl = featuredImageUrl;
    }

    public void Publish(ISystemClock clock)
    {
        if (IsPublished) return;
        PublishedOn = clock.UtcNow;
        RaiseDomainEvent(new NewsPublishedEvent(Id, Slug, PublishedOn.Value));
    }

    public void MarkFeatured() => IsFeatured = true;

    public void UnmarkFeatured() => IsFeatured = false;
}
