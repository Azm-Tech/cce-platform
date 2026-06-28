using CCE.Application.Common.Realtime;
using CCE.Application.Notifications;
using CCE.Domain.Notifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications;

public sealed class SignalRNotificationPublisher : ISignalRNotificationPublisher
{
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly ILogger<SignalRNotificationPublisher> _logger;

    public SignalRNotificationPublisher(
        IHubContext<NotificationsHub> hubContext,
        ILogger<SignalRNotificationPublisher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PublishAsync(UserNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Publishing notification {NotificationId} to user {UserId}",
            notification.Id,
            notification.UserId);

        await _hubContext
            .Clients
            .User(notification.UserId.ToString())
            .SendAsync(
                RealtimeEvents.ReceiveNotification,
                RealtimeEnvelope.Wrap(new
                {
                    notification.Id,
                    notification.TemplateId,
                    notification.RenderedSubjectAr,
                    notification.RenderedSubjectEn,
                    notification.RenderedBody,
                    notification.RenderedLocale,
                    notification.Status,
                    notification.SentOn,
                    actorId = notification.ActorId,
                    metaData = notification.MetaData,
                }),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
