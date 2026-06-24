using CCE.Application.Messages;
using CCE.Application.Notifications.Messages;
using CCE.Domain.Identity.Events;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Handlers;

public sealed class ExpertRegistrationRejectedNotificationHandler
    : INotificationHandler<ExpertRegistrationRejectedEvent>
{
    private readonly INotificationMessageDispatcher _dispatcher;

    public ExpertRegistrationRejectedNotificationHandler(INotificationMessageDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task Handle(
        ExpertRegistrationRejectedEvent notification,
        CancellationToken cancellationToken)
    {
        await _dispatcher.DispatchAsync(new NotificationMessage(
            TemplateCode: MessageKeys.Identity.EXPERT_REQUEST_REJECTED,
            RecipientUserId: notification.RequestedById,
            EventType: NotificationEventType.ExpertRequestRejected,
            Channels: [NotificationChannel.InApp, NotificationChannel.Email],
            MetaData: new Dictionary<string, string>
            {
                ["Reason"] = notification.RejectionReasonEn ?? ""
            },
            Locale: "en"), cancellationToken).ConfigureAwait(false);
    }
}
