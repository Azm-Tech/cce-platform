using CCE.Domain.Identity;

namespace CCE.Application.Reports.Dtos;

public sealed record UserPreferenceReportDto(
    Guid Id,
    List<AreaOfInterestDto> AreasOfInterest,
    KnowledgeLevel KnowledgeLevel,
    string SectorOfWork,
    Guid? CountryId,
    string? CountryNameAr,
    string? CountryNameEn
);
