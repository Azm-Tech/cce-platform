namespace CCE.Application.Notifications;

public interface INotificationGateway
{
    Task<NotificationDispatchResult> SendAsync(
        NotificationDispatchRequest request,
        CancellationToken cancellationToken);
}
