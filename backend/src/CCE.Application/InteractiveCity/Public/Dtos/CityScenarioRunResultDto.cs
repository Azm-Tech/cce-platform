namespace CCE.Application.InteractiveCity.Public.Dtos;

public sealed record CityScenarioRunResultDto(
    decimal TotalCarbonImpactKgPerYear,
    decimal TotalCostUsd,
    string SummaryAr,
    string SummaryEn);
