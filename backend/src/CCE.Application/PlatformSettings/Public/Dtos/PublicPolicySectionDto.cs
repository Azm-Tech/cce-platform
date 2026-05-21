namespace CCE.Application.PlatformSettings.Public.Dtos;

public sealed record PublicPolicySectionDto(
    int Type,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn);
