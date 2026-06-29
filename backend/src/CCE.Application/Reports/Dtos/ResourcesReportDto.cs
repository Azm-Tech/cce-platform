using CCE.Domain.Content;

namespace CCE.Application.Reports.Dtos;

public sealed record ResourcesReportDto(
    Guid Id,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    Guid CategoryId,
    string CategoryNameAr,
    string CategoryNameEn,
    ResourceType PostType,
    Guid[] CoveredCountries,
    DateTimeOffset CreatedAt
);
