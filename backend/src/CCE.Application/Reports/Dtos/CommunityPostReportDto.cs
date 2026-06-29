using CCE.Domain.Community;

namespace CCE.Application.Reports.Dtos;

public sealed record CommunityPostReportDto(
    Guid Id,
    string? PostTitle,
    string? PostContent,
    PostType PostType,
    DateTimeOffset CreatedAt
);
