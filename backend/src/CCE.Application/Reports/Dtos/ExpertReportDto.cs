namespace CCE.Application.Reports.Dtos;

public sealed record ExpertReportDto(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string? Email,
    string JobTitle,
    string OrganizationName,
    string CvDescriptionEn,
    string CvDescriptionAr,
    string? CvAttachmentUrl,
    string CvFileFormat,
    List<string> ExpertiseTopics,
    int Status,
    DateTimeOffset SubmittedAt
);
