using CCE.Domain.Notifications;

namespace CCE.Application.Notifications.Public.Dtos;

public sealed record UserNotificationDto(
    System.Guid Id,
    System.Guid TemplateId,
    string RenderedSubjectAr,
    string RenderedSubjectEn,
    string RenderedBody,
    string RenderedLocale,
    NotificationChannel Channel,
    System.DateTimeOffset? SentOn,
    System.DateTimeOffset? ReadOn,
    NotificationStatus Status);
