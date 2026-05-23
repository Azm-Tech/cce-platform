using CCE.Domain.Notifications;

namespace CCE.Application.Notifications;

public sealed record ChannelSendResult(
    bool Success,
    string? ProviderMessageId = null,
    string? Error = null,
    System.Guid? UserNotificationId = null,
    UserNotification? UserNotification = null);
