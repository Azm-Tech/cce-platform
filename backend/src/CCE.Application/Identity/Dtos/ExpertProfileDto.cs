namespace CCE.Application.Identity.Dtos;

public sealed record ExpertProfileDto(
    System.Guid Id,
    System.Guid UserId,
    string? UserName,
    string BioAr,
    string BioEn,
    IReadOnlyList<string> ExpertiseTags,
    string AcademicTitleAr,
    string AcademicTitleEn,
    System.DateTimeOffset ApprovedOn,
    System.Guid ApprovedById);
