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
    System.Guid LastUpdatedById,
    System.DateTimeOffset LastUpdatedOn,
    string RowVersion);
