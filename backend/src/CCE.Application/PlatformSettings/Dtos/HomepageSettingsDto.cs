namespace CCE.Application.PlatformSettings.Dtos;

public sealed record HomepageSettingsDto(
    System.Guid Id,
    string? VideoUrl,
    LocalizedTextDto Objective,
    string CceConceptsAr,
    string CceConceptsEn,
    System.Collections.Generic.IReadOnlyList<HomepageCountryDto> ParticipatingCountries);

public sealed record HomepageCountryDto(
    System.Guid Id,
    System.Guid CountryId,
    int OrderIndex);
