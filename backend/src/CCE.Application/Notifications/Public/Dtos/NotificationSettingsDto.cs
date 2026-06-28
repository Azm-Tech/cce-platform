using CCE.Domain.Notifications;

namespace CCE.Application.Notifications.Public.Dtos;

public sealed record NotificationSettingsDto(
    NotificationChannel Channel,
    string? EventCode,
    bool IsEnabled);
