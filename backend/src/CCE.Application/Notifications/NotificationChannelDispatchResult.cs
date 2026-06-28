using CCE.Domain.Notifications;

namespace CCE.Application.Notifications;

public sealed record NotificationChannelDispatchResult(
    NotificationChannel Channel,
    NotificationDeliveryStatus Status,
    Guid? NotificationLogId = null,
    Guid? UserNotificationId = null,
    string? ProviderMessageId = null,
    string? Error = null);
