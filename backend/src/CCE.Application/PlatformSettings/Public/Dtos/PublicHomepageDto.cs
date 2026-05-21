using CCE.Application.Content.Public.Dtos;

namespace CCE.Application.PlatformSettings.Public.Dtos;

public sealed record PublicHomepageDto(
    string? VideoUrl,
    string ObjectiveAr,
    string ObjectiveEn,
    string CceConceptsAr,
    string CceConceptsEn,
    System.Collections.Generic.IReadOnlyList<PublicHomepageCountryDto> ParticipatingCountries,
    System.Collections.Generic.IReadOnlyList<PublicHomepageSectionDto> Sections);
