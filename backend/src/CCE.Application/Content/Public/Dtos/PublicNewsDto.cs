using CCE.Application.Content.Dtos;

namespace CCE.Application.Content.Public.Dtos;

public sealed record PublicNewsDto(
    System.Guid Id,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn,
    System.Guid TopicId,
    string TopicNameAr,
    string TopicNameEn,
    string? FeaturedImageUrl,
    System.DateTimeOffset PublishedOn,
    bool IsFeatured,
    System.Collections.Generic.IReadOnlyList<TagDto> Tags);
