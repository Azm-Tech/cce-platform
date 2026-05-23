using CCE.Application.Content.Public.Dtos;
using CCE.Application.PlatformSettings.Dtos;

namespace CCE.Application.PlatformSettings.Public.Dtos;

public sealed record PublicHomepageDto(
    string? VideoUrl,
    LocalizedTextDto Objective,
    string CceConceptsAr,
    string CceConceptsEn,
    System.Collections.Generic.IReadOnlyList<PublicHomepageCountryDto> ParticipatingCountries,
    System.Collections.Generic.IReadOnlyList<PublicHomepageSectionDto> Sections);
