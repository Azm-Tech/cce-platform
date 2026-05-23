using CCE.Domain.Notifications;

namespace CCE.Api.External.Endpoints;

public sealed record UpdateMyNotificationSettingsRequest(
    NotificationChannel Channel,
    bool IsEnabled,
    string? EventCode = null);
