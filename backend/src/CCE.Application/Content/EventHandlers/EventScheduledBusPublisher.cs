using CCE.Application.Common.Messaging;
using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Domain.Content.Events;
using MediatR;

namespace CCE.Application.Content.EventHandlers;

/// <summary>
/// Bridge: translates the <see cref="EventScheduledEvent"/> domain event into an
/// <see cref="EventScheduledIntegrationEvent"/> on the bus. Runs pre-commit inside
/// <c>DomainEventDispatcher</c>, so the publish is captured by the MassTransit EF outbox
/// and committed atomically with the aggregate save. The Worker's
/// <c>ContentNotificationConsumer</c> fans out to newsletter subscribers.
/// </summary>
public sealed class EventScheduledBusPublisher : INotificationHandler<EventScheduledEvent>
{
    private readonly IIntegrationEventPublisher _publisher;

    public EventScheduledBusPublisher(IIntegrationEventPublisher publisher)
        => _publisher = publisher;

    public Task Handle(EventScheduledEvent notification, CancellationToken cancellationToken)
        => _publisher.PublishAsync(new EventScheduledIntegrationEvent(
            notification.EventId,
            notification.TopicId,
            notification.StartsOn,
            notification.EndsOn,
            notification.OccurredOn), cancellationToken);
}
