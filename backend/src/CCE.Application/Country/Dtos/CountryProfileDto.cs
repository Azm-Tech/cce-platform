namespace CCE.Application.Country.Dtos;

public sealed record CountryProfileDto(
    System.Guid Id,
    System.Guid CountryId,
    string DescriptionAr,
    string DescriptionEn,
    string KeyInitiativesAr,
    string KeyInitiativesEn,
    string? ContactInfoAr,
    string? ContactInfoEn,
    int? Population,
    decimal? AreaSqKm,
    decimal? GdpPerCapita,
    System.Guid? NdcAssetId,
    string? CceClassification,
    decimal? CcePerformanceScore,
    decimal? CceTotalIndex,
    System.DateTimeOffset? CceSnapshotTakenOn,
    System.Guid LastUpdatedById,
    System.DateTimeOffset LastUpdatedOn);
