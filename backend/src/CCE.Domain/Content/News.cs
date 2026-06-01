using CCE.Domain.Common;
using CCE.Domain.Content.Events;

namespace CCE.Domain.Content;

/// <summary>
/// News article — bilingual title + rich-text content + optional featured image.
/// Slug is auto-generated from the English title. Soft-deletable, audited.
/// </summary>
[Audited]
public sealed class News : AggregateRoot<System.Guid>
{
    private News(
        System.Guid id,
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn,
        string slug,
        System.Guid topicId,
        System.Guid authorId,
        string? featuredImageUrl) : base(id)
    {
        TitleAr = titleAr;
        TitleEn = titleEn;
        ContentAr = contentAr;
        ContentEn = contentEn;
        Slug = slug;
        TopicId = topicId;
        AuthorId = authorId;
        FeaturedImageUrl = featuredImageUrl;
    }

    public string TitleAr { get; private set; }
    public string TitleEn { get; private set; }
    public string ContentAr { get; private set; }
    public string ContentEn { get; private set; }
    public string Slug { get; private set; }
    public System.Guid TopicId { get; private set; }
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
        System.Guid topicId,
        System.Guid authorId,
        string? featuredImageUrl,
        ISystemClock clock)
    {
        _ = clock;
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(contentAr)) throw new DomainException("ContentAr is required.");
        if (string.IsNullOrWhiteSpace(contentEn)) throw new DomainException("ContentEn is required.");
        if (topicId == System.Guid.Empty) throw new DomainException("TopicId is required.");
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
            slug: ToKebabSlug(titleEn),
            topicId: topicId,
            authorId: authorId,
            featuredImageUrl: featuredImageUrl);
    }

    public void UpdateContent(
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn,
        System.Guid topicId,
        string? featuredImageUrl)
    {
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(contentAr)) throw new DomainException("ContentAr is required.");
        if (string.IsNullOrWhiteSpace(contentEn)) throw new DomainException("ContentEn is required.");
        if (topicId == System.Guid.Empty) throw new DomainException("TopicId is required.");
        if (featuredImageUrl is not null
            && !featuredImageUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("FeaturedImageUrl must use https://.");
        }
        TitleAr = titleAr;
        TitleEn = titleEn;
        ContentAr = contentAr;
        ContentEn = contentEn;
        TopicId = topicId;
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

    private static string ToKebabSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "news";
        var sb = new System.Text.StringBuilder();
        bool lastWasHyphen = true;
        foreach (var c in text.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen)
            {
                sb.Append('-');
                lastWasHyphen = true;
            }
        }
        if (lastWasHyphen && sb.Length > 0)
            sb.Length--;
        var result = sb.ToString();
        return string.IsNullOrWhiteSpace(result) ? "news" : result;
    }
}
