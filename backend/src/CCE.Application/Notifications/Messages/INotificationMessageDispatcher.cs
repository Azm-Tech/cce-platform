namespace CCE.Application.Notifications.Messages;

public interface INotificationMessageDispatcher
{
    Task DispatchAsync(NotificationMessage message, CancellationToken ct);
}
