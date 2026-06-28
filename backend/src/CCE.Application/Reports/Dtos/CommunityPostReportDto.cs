namespace CCE.Application.Reports.Dtos;

public sealed record CommunityPostReportDto(
    Guid Id,
    string? PostTitle,
    string? PostContent,
    int PostType,
    DateTimeOffset CreatedAt
);
