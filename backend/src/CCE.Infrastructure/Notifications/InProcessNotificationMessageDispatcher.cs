using CCE.Application.Notifications;
using CCE.Application.Notifications.Messages;

namespace CCE.Infrastructure.Notifications;

public sealed class InProcessNotificationMessageDispatcher : INotificationMessageDispatcher
{
    private readonly INotificationGateway _gateway;

    public InProcessNotificationMessageDispatcher(INotificationGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task DispatchAsync(NotificationMessage message, CancellationToken ct)
    {
        await _gateway.SendAsync(new NotificationDispatchRequest(
            TemplateCode: message.TemplateCode,
            RecipientUserId: message.RecipientUserId,
            Channels: message.Channels ?? [],
            Variables: message.MetaData,
            Locale: message.Locale,
            Email: message.Email,
            PhoneNumber: message.PhoneNumber,
            CorrelationId: message.CorrelationId), ct).ConfigureAwait(false);
    }
}
