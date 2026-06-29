namespace CCE.Application.Reports.Dtos;

public sealed record CountryProfilesReportDto(
    Guid Id,
    string CountryNameAr,
    string CountryNameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string? KeyInitiativesAr,
    string? KeyInitiativesEn,
    int? Population,
    decimal? Area,
    decimal? GdpPerCapita,
    string? NdcAttachmentUrl,
    string? CceClassification,
    decimal? CcePerformanceIndex
);
