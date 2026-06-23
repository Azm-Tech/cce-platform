namespace CCE.Application.Reports.Dtos;

public sealed record EventsReportDto(
    Guid Id,
    string Title,
    string EventDescription,
    string? Location,
    string Topic,
    DateTimeOffset StartsOn,
    DateTimeOffset EndsOn,
    string? FeaturedImageUrl,
    string? OnlineMeetingUrl,
    DateTimeOffset CreatedAt
);
