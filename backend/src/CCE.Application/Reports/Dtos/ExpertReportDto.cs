namespace CCE.Application.Reports.Dtos;

public sealed record ExpertReportDto(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string? Email,
    string? JobTitle,
    string? OrganizationName,
    string BioAr,
    string BioEn,
    string AcademicTitleAr,
    string AcademicTitleEn,
    List<string> ExpertiseTopics,
    DateTimeOffset ApprovedOn
);
