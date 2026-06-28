using CCE.Application.Common;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Public.Commands.UpdateMyNotificationSettings;

public sealed record UpdateMyNotificationSettingsCommand(
    System.Guid UserId,
    NotificationChannel Channel,
    bool IsEnabled,
    string? EventCode = null) : IRequest<Response<VoidData>>;
