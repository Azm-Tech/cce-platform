using CCE.Domain.Content;

namespace CCE.Application.Content.Public.Dtos;

public sealed record HomepageFeedItemDto(
    System.Guid Id,
    int ContentTypeId,
    HomepageFeedContentType ContentType,
    string NameEn,
    string NameAr,
    System.Guid TopicId,
    string TopicNameEn,
    string TopicNameAr,
    System.Guid? AuthorId,
    string? AuthorName,
    string? FeaturedImageUrl,
    string? LocationEn,
    string? LocationAr,
    System.DateTimeOffset PublishedOn);
