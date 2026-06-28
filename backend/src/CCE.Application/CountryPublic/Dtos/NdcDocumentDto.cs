namespace CCE.Application.CountryPublic.Dtos;

/// <summary>
/// Nationally Determined Contribution document info surfaced on the public country profile.
/// The AssetId can be used by the client to call GET /api/countries/{countryId}/ndc for download.
/// </summary>
public sealed record NdcDocumentDto(System.Guid AssetId, string OriginalFileName);
