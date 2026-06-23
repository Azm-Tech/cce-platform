namespace CCE.Application.Reports.Dtos;

public sealed record CountryProfilesReportDto(
    Guid Id,
    string CountryName,
    int? Population,
    decimal? Area,
    decimal? GdpPerCapita,
    string? NdcAttachmentUrl,
    string? CceClassification,
    decimal? CcePerformanceIndex
);
