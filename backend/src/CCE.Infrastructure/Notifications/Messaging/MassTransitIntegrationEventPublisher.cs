using CCE.Application.Common.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications.Messaging;

/// <summary>
/// MassTransit-backed <see cref="IIntegrationEventPublisher"/>. Publishes onto the bus via
/// the scoped bus context provider so that when the EF bus outbox is configured (see
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
    private readonly ILogger<MassTransitIntegrationEventPublisher> _logger;

    public MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint, ILogger<MassTransitIntegrationEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken)
        where T : class
    {
        _logger.LogInformation("MassTransitIntegrationEventPublisher.PublishAsync: type={Type}, endpointType={EndpointType}", typeof(T).Name, _publishEndpoint.GetType().FullName);
        return _publishEndpoint.Publish(@event, cancellationToken);
    }
}
