using CCE.Domain.Notifications;

namespace CCE.Application.Notifications;

public sealed record RenderedNotification(
    string TemplateCode,
    System.Guid? RecipientUserId,
    System.Guid TemplateId,
    string Subject,
    string SubjectAr,
    string SubjectEn,
    string Body,
    NotificationChannel Channel,
    string Locale,
    string? Email = null,
    string? PhoneNumber = null,
    System.Collections.Generic.IReadOnlyDictionary<string, string>? MetaData = null);
