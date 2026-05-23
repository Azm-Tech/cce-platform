using CCE.Application.PlatformSettings.Dtos;

namespace CCE.Application.PlatformSettings.Public.Dtos;

public sealed record PublicGlossaryEntryDto(
    LocalizedTextDto Term,
    LocalizedTextDto Definition);
