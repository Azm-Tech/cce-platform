using CCE.Application.Common.Messaging;
using MassTransit;

namespace CCE.Infrastructure.Notifications.Messaging;

/// <summary>
/// MassTransit-backed <see cref="IIntegrationEventPublisher"/>. Publishes onto the bus via
/// <see cref="IPublishEndpoint"/>; when the EF bus outbox is configured (see
/// <c>MessagingServiceExtensions.AddCceMessaging</c>) the publish is captured into the
/// <c>outbox_message</c> table within the caller's <c>CceDbContext</c> transaction and relayed to the
/// broker after <c>SaveChanges</c> commits.
///
/// <para>Sibling of <see cref="MassTransitNotificationMessageDispatcher"/>, which does the same for the
/// notification-specific <c>NotificationMessage</c> contract.</para>
/// </summary>
public sealed class MassTransitIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
        => _publishEndpoint = publishEndpoint;

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken)
        where T : class
        => _publishEndpoint.Publish(@event, cancellationToken);
}
