namespace CCE.Application.Reports.Dtos;

public sealed record ResourcesReportDto(
    Guid Id,
    string Title,
    string Description,
    Guid CategoryId,
    string Category,
    int PostType,
    Guid[] CoveredCountries,
    DateTimeOffset CreatedAt
);
