namespace CCE.Application.Reports.Dtos;

public sealed record UserRegistrationReportDto(
    string ReportId,
    string ReportTitle,
    DateTimeOffset GeneratedAt,
    int TotalUsers,
    IReadOnlyList<UserRegistrationReportUserDto> Users
);
