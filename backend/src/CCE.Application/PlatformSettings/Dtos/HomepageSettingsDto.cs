namespace CCE.Application.PlatformSettings.Dtos;

public sealed record HomepageSettingsDto(
    System.Guid Id,
    string? VideoUrl,
    string ObjectiveAr,
    string ObjectiveEn,
    string CceConceptsAr,
    string CceConceptsEn,
    System.Collections.Generic.IReadOnlyList<HomepageCountryDto> ParticipatingCountries,
    string RowVersion);

public sealed record HomepageCountryDto(
    System.Guid Id,
    System.Guid CountryId,
    int OrderIndex);
