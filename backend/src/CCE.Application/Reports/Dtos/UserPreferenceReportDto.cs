namespace CCE.Application.Reports.Dtos;

public sealed record UserPreferenceReportDto(
    Guid Id,
    List<Guid> AreasOfInterest,
    int KnowledgeLevel,
    string SectorOfWork,
    Guid? CountryId
);
