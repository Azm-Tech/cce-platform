namespace CCE.Application.Reports.Dtos;

public sealed record EventsReportDto(
    Guid Id,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    string? LocationAr,
    string? LocationEn,
    string TopicNameAr,
    string TopicNameEn,
    DateTimeOffset StartsOn,
    DateTimeOffset EndsOn,
    string? FeaturedImageUrl,
    string? OnlineMeetingUrl,
    DateTimeOffset CreatedAt
);
