using CCE.Application.Common.Interfaces;
using CCE.Application.Notifications;
using CCE.Application.Notifications.Public;
using CCE.Domain.Common;
using CCE.Domain.Notifications;

namespace CCE.Infrastructure.Notifications;

public sealed class InAppNotificationChannelSender : INotificationChannelHandler
{
    private readonly IUserNotificationRepository _repo;
    private readonly ISystemClock _clock;

    public InAppNotificationChannelSender(IUserNotificationRepository repo, ISystemClock clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public NotificationChannel Channel => NotificationChannel.InApp;

    public bool ShouldSend(UserNotificationSettings? settings) => settings?.IsEnabled ?? true;

    public async Task<ChannelSendResult> SendAsync(
        RenderedNotification notification,
        CancellationToken cancellationToken)
    {
        if (notification.RecipientUserId is null)
        {
            return new ChannelSendResult(
                false, Error: "In-app notifications require a recipient user ID.");
        }

        var userNotification = UserNotification.Render(
            notification.RecipientUserId.Value,
            notification.TemplateId,
            notification.SubjectAr,
            notification.SubjectEn,
            notification.Body,
            notification.Locale,
            NotificationChannel.InApp);

        userNotification.MarkSent(_clock);
        await _repo.AddAsync(userNotification, cancellationToken).ConfigureAwait(false);

        return new ChannelSendResult(
            true,
            UserNotificationId: userNotification.Id,
            UserNotification: userNotification);
    }
}
