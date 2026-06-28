namespace CCE.Application.CountryPublic.Dtos;

public sealed record PublicCountryDto(
    System.Guid Id,
    string? IsoAlpha3,
    string? IsoAlpha2,
    string NameAr,
    string NameEn,
    string? RegionAr,
    string? RegionEn,
    string FlagUrl,
    string? DialCode,
    bool IsCceCountry,
    string? CceClassification,
    decimal? CcePerformanceScore,
    decimal? CceTotalIndex);
