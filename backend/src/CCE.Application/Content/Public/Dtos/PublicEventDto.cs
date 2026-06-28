using CCE.Application.Content.Dtos;

namespace CCE.Application.Content.Public.Dtos;

public sealed record PublicEventDto(
    System.Guid Id,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    System.DateTimeOffset StartsOn,
    System.DateTimeOffset EndsOn,
    string? LocationAr,
    string? LocationEn,
    string? OnlineMeetingUrl,
    string? FeaturedImageUrl,
    string ICalUid,
    System.Guid TopicId,
    string TopicNameAr,
    string TopicNameEn,
    System.Collections.Generic.IReadOnlyList<TagDto> Tags,
    System.Guid? KnowledgeLevelId,
    System.Guid? JobSectorId);
