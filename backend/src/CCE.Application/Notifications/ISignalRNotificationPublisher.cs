using CCE.Domain.Notifications;

namespace CCE.Application.Notifications;

/// <summary>
/// Publishes a persisted in-app notification to real-time subscribers via SignalR.
/// </summary>
public interface ISignalRNotificationPublisher
{
    Task PublishAsync(UserNotification notification, CancellationToken cancellationToken);
}
