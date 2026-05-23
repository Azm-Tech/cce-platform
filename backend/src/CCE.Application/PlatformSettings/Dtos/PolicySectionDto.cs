namespace CCE.Application.PlatformSettings.Dtos;

public sealed record PolicySectionDto(
    System.Guid Id,
    int Type,
    LocalizedTextDto Title,
    LocalizedTextDto Content,
    int OrderIndex);
