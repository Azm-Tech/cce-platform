using CCE.Domain.Notifications;

namespace CCE.Application.Notifications;

public sealed record NotificationDispatchRequest(
    string TemplateCode,
    Guid? RecipientUserId,
    IReadOnlyCollection<NotificationChannel> Channels,
    IReadOnlyDictionary<string, string>? Variables = null,
    string Locale = "en",
    string? Email = null,
    string? PhoneNumber = null,
    string? Source = null,
    string? CorrelationId = null,
    string? DeduplicationKey = null,
    bool BypassSettings = false);
