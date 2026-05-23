using CCE.Domain.Notifications;

namespace CCE.Application.Notifications.Admin.Queries.GetNotificationLogById;

public sealed record NotificationLogDto(
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
    string? CorrelationId,
    string? PayloadJson);
