using CCE.Application.Notifications.Messages;
using CCE.Domain.Country.Events;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Handlers;

public sealed class CountryContentRequestRejectedNotificationHandler
    : INotificationHandler<CountryContentRequestRejectedEvent>
{
    private readonly INotificationMessageDispatcher _dispatcher;

    public CountryContentRequestRejectedNotificationHandler(INotificationMessageDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task Handle(
        CountryContentRequestRejectedEvent notification,
        CancellationToken cancellationToken)
    {
        await _dispatcher.DispatchAsync(new NotificationMessage(
            TemplateCode: "COUNTRY_CONTENT_REQUEST_REJECTED",
            RecipientUserId: notification.RequestedById,
            EventType: NotificationEventType.CountryResourceRejected,
            Channels: [NotificationChannel.InApp, NotificationChannel.Email],
            MetaData: new Dictionary<string, string>
            {
                ["RequestId"] = notification.RequestId.ToString(),
                ["Kind"] = notification.Kind.ToString(),
                ["AdminNotesAr"] = notification.AdminNotesAr,
                ["AdminNotesEn"] = notification.AdminNotesEn,
            }),
            cancellationToken).ConfigureAwait(false);
    }
}
