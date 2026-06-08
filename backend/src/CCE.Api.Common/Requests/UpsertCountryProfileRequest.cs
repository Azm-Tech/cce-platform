namespace CCE.Api.Common.Requests;

public sealed record UpsertCountryProfileRequest(
    string DescriptionAr,
    string DescriptionEn,
    string KeyInitiativesAr,
    string KeyInitiativesEn,
    string? ContactInfoAr,
    string? ContactInfoEn,
    int? Population,
    decimal? AreaSqKm,
    decimal? GdpPerCapita,
    System.Guid? NdcAssetId);
