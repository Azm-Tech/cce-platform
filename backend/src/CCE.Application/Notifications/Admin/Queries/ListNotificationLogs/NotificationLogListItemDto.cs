using CCE.Domain.Notifications;

namespace CCE.Application.Notifications.Admin.Queries.ListNotificationLogs;

public sealed record NotificationLogListItemDto(
    System.Guid Id,
    System.Guid? RecipientUserId,
    string TemplateCode,
    System.Guid? TemplateId,
    NotificationChannel Channel,
    NotificationDeliveryStatus Status,
    string? ProviderMessageId,
    string? Error,
    int AttemptCount,
    System.DateTimeOffset CreatedOn,
    System.DateTimeOffset? SentOn,
    System.DateTimeOffset? FailedOn,
    string? CorrelationId);
