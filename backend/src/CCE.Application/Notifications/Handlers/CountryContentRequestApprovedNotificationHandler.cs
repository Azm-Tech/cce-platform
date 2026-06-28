using CCE.Application.Notifications.Messages;
using CCE.Domain.Country.Events;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Handlers;

public sealed class CountryContentRequestApprovedNotificationHandler
    : INotificationHandler<CountryContentRequestApprovedEvent>
{
    private readonly INotificationMessageDispatcher _dispatcher;

    public CountryContentRequestApprovedNotificationHandler(INotificationMessageDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task Handle(
        CountryContentRequestApprovedEvent notification,
        CancellationToken cancellationToken)
    {
        await _dispatcher.DispatchAsync(new NotificationMessage(
            TemplateCode: "COUNTRY_CONTENT_REQUEST_APPROVED",
            RecipientUserId: notification.RequestedById,
            EventType: NotificationEventType.CountryResourceApproved,
            Channels: [NotificationChannel.InApp, NotificationChannel.Email, NotificationChannel.Push],
            MetaData: new Dictionary<string, string>
            {
                ["RequestId"] = notification.RequestId.ToString(),
                ["Type"] = notification.Type.ToString(),
            }),
            cancellationToken).ConfigureAwait(false);
    }
}
