namespace CCE.Application.Community.Public.Dtos;

/// <summary>
/// A popular community post for the public featured-posts feed.
/// A post is single-language free text, so <see cref="NameAr"/>/<see cref="NameEn"/>
/// carry the post's Topic name (the natural bilingual label); <see cref="Content"/> is
/// the post body and <see cref="PublishedOn"/> is its creation time.
/// </summary>
public sealed record FeaturedPostDto(
    System.Guid Id,
    System.Guid TopicId,
    string NameAr,
    string NameEn,
    string Content,
    string Locale,
    System.Guid AuthorId,
    string? PublishedByName,
    System.DateTimeOffset PublishedOn,
    int RatingCount,
    double AverageStars,
    int ReplyCount);
