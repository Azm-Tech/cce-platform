using CCE.Application.Messages;
using CCE.Application.Notifications.Messages;
using CCE.Domain.Identity.Events;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Handlers;

public sealed class ExpertRegistrationApprovedNotificationHandler
    : INotificationHandler<ExpertRegistrationApprovedEvent>
{
    private readonly INotificationMessageDispatcher _dispatcher;

    public ExpertRegistrationApprovedNotificationHandler(INotificationMessageDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task Handle(
        ExpertRegistrationApprovedEvent notification,
        CancellationToken cancellationToken)
    {
        await _dispatcher.DispatchAsync(new NotificationMessage(
            TemplateCode: MessageKeys.Identity.EXPERT_REQUEST_APPROVED,
            RecipientUserId: notification.RequestedById,
            EventType: NotificationEventType.ExpertRequestApproved,
            Channels: [NotificationChannel.InApp, NotificationChannel.Email, NotificationChannel.Push],
            MetaData: new Dictionary<string, string>(),
            Locale: "en"), cancellationToken).ConfigureAwait(false);
    }
}
