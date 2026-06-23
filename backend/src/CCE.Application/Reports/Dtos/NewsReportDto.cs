namespace CCE.Application.Reports.Dtos;

public sealed record NewsReportDto(
    Guid Id,
    string TitleAr,
    string TitleEn,
    string? ImageUrl,
    string TopicNameAr,
    string TopicNameEn,
    string ContentAr,
    string ContentEn,
    DateTimeOffset? PublishedAt
);
