namespace CCE.Application.CountryPublic.Dtos;

public sealed record PublicCountryProfileDto(
    System.Guid Id,
    System.Guid CountryId,
    string DescriptionAr,
    string DescriptionEn,
    string KeyInitiativesAr,
    string KeyInitiativesEn,
    string? ContactInfoAr,
    string? ContactInfoEn,
    System.DateTimeOffset LastUpdatedOn);
