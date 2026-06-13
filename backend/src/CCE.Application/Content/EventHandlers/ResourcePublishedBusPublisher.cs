using CCE.Application.Common.Messaging;
using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Domain.Content.Events;
using MediatR;

namespace CCE.Application.Content.EventHandlers;

/// <summary>
/// Bridge: translates the <see cref="ResourcePublishedEvent"/> domain event into a
/// <see cref="ResourcePublishedIntegrationEvent"/> on the bus. Runs pre-commit inside
/// <c>DomainEventDispatcher</c>, so the publish is captured by the MassTransit EF outbox
/// and committed atomically with the aggregate save. The Worker's
/// <c>ContentNotificationConsumer</c> fans out to newsletter subscribers.
/// </summary>
public sealed class ResourcePublishedBusPublisher : INotificationHandler<ResourcePublishedEvent>
{
    private readonly IIntegrationEventPublisher _publisher;

    public ResourcePublishedBusPublisher(IIntegrationEventPublisher publisher)
        => _publisher = publisher;

    public Task Handle(ResourcePublishedEvent notification, CancellationToken cancellationToken)
        => _publisher.PublishAsync(new ResourcePublishedIntegrationEvent(
            notification.ResourceId,
            notification.CategoryId,
            notification.CountryId,
            notification.UploadedById,
            notification.OccurredOn), cancellationToken);
}
