namespace CCE.Application.InteractiveCity.Public.Dtos;

public sealed record CityTechnologyDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string DescriptionAr,
    string DescriptionEn,
    string CategoryAr,
    string CategoryEn,
    decimal CarbonImpactKgPerYear,
    decimal CostUsd,
    string? IconUrl);
