using CCE.Application.Notifications.Messages;
using MassTransit;

namespace CCE.Infrastructure.Notifications.Messaging;

/// <summary>
/// Drop-in replacement for <see cref="InProcessNotificationMessageDispatcher"/>.
/// Instead of calling <c>INotificationGateway</c> inline it publishes a
/// <see cref="NotificationMessage"/> onto the MassTransit bus so the work
/// is handled asynchronously by <see cref="NotificationMessageConsumer"/>
/// (which may run in this process, or in a separate worker process).
///
/// <para>
/// Wire-up: replace the <c>InProcessNotificationMessageDispatcher</c> DI
/// registration with this class.  See <c>MessagingServiceExtensions</c>.
/// </para>
/// </summary>
public sealed class MassTransitNotificationMessageDispatcher : INotificationMessageDispatcher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitNotificationMessageDispatcher(IPublishEndpoint publishEndpoint)
        => _publishEndpoint = publishEndpoint;

    public Task DispatchAsync(NotificationMessage message, CancellationToken ct)
        => _publishEndpoint.Publish(message, ct);
}
