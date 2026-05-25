using CCE.Domain.Notifications;

namespace CCE.Application.Notifications.Messages;

public sealed record NotificationMessage(
    string TemplateCode,
    System.Guid? RecipientUserId,
    NotificationEventType EventType,
    IReadOnlyDictionary<string, string>? MetaData = null,
    IReadOnlyCollection<NotificationChannel>? Channels = null,
    string Locale = "en",
    string? Email = null,
    string? PhoneNumber = null,
    string? CorrelationId = null);
