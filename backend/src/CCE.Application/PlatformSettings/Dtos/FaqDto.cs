namespace CCE.Application.PlatformSettings.Dtos;

public sealed record FaqDto(
    System.Guid Id,
    LocalizedTextDto Question,
    LocalizedTextDto Answer,
    int Order);
