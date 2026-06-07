using CCE.Application.PlatformSettings.Dtos;

namespace CCE.Application.Lookups;

public sealed record CountryCodeDto(
    System.Guid Id,
    LocalizedTextDto Name,
    string DialCode,
    string? FlagUrl,
    bool IsActive);
