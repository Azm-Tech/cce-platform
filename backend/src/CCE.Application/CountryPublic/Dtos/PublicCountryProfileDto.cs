namespace CCE.Application.CountryPublic.Dtos;

/// <summary>
/// Full state-profile detail returned by GET /api/countries/{id}/profile (US014 AC5).
/// Includes country identity fields so the response is self-contained.
/// Editorial fields are nullable — a country may exist with KAPSARC data before the
/// state rep has filled in the editorial content.
/// </summary>
public sealed record PublicCountryProfileDto(
    System.Guid CountryId,
    // Country identity
    string IsoAlpha3,
    string NameAr,
    string NameEn,
    string FlagUrl,
    // Editorial content (null until set by state rep / admin)
    string? DescriptionAr,
    string? DescriptionEn,
    string? KeyInitiativesAr,
    string? KeyInitiativesEn,
    string? ContactInfoAr,
    string? ContactInfoEn,
    // Demographic / economic (null until set)
    int? Population,
    decimal? AreaSqKm,
    decimal? GdpPerCapita,
    // NDC document (null until uploaded)
    NdcDocumentDto? NdcDocument,
    // KAPSARC read-only metrics (null when no snapshot available)
    string? CceClassification,
    decimal? CcePerformanceScore,
    decimal? CceTotalIndex,
    System.DateTimeOffset? CceSnapshotTakenOn,
    System.DateTimeOffset? LastUpdatedOn);
