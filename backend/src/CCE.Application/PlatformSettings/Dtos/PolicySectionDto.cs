namespace CCE.Application.PlatformSettings.Dtos;

public sealed record PolicySectionDto(
    System.Guid Id,
    int Type,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn,
    int OrderIndex);
