using CCE.Application.PlatformSettings.Dtos;

namespace CCE.Application.PlatformSettings.Public.Dtos;

public sealed record PublicFaqDto(
    LocalizedTextDto Question,
    LocalizedTextDto Answer,
    int Order);
