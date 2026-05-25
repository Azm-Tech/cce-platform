namespace CCE.Application.PlatformSettings.Public.Dtos;

public sealed record PublicHomepageCountryDto(
    System.Guid Id,
    string IsoAlpha3,
    string NameAr,
    string NameEn,
    string FlagUrl,
    int OrderIndex);
