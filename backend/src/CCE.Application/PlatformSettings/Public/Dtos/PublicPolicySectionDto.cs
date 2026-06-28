using CCE.Application.PlatformSettings.Dtos;

namespace CCE.Application.PlatformSettings.Public.Dtos;

public sealed record PublicPolicySectionDto(
    int Type,
    LocalizedTextDto Title,
    LocalizedTextDto Content);
