namespace CCE.Application.Country.Dtos;

public sealed record CountryDto(
    System.Guid Id,
    string IsoAlpha3,
    string IsoAlpha2,
    string NameAr,
    string NameEn,
    string RegionAr,
    string RegionEn,
    string FlagUrl,
    bool IsActive);
