namespace CCE.Application.PlatformSettings.Dtos;

public sealed record GlossaryEntryDto(
    System.Guid Id,
    LocalizedTextDto Term,
    LocalizedTextDto Definition,
    int OrderIndex);
