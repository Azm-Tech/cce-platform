using CCE.Domain.Notifications;

namespace CCE.Application.Notifications;

public interface INotificationChannelHandler
{
    NotificationChannel Channel { get; }

    bool ShouldSend(UserNotificationSettings? settings);

    Task<ChannelSendResult> SendAsync(
        RenderedNotification notification,
        CancellationToken cancellationToken);
}
