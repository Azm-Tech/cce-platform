using CCE.Application.Common.Messaging;
using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Domain.Content.Events;
using MediatR;

namespace CCE.Application.Content.EventHandlers;

/// <summary>
/// Bridge: translates the <see cref="NewsPublishedEvent"/> domain event into a
/// <see cref="NewsPublishedIntegrationEvent"/> on the bus. Runs pre-commit inside
/// <c>DomainEventDispatcher</c>, so the publish is captured by the MassTransit EF outbox
/// and committed atomically with the aggregate save. The Worker's
/// <c>ContentNotificationConsumer</c> fans out to newsletter subscribers.
/// </summary>
public sealed class NewsPublishedBusPublisher : INotificationHandler<NewsPublishedEvent>
{
    private readonly IIntegrationEventPublisher _publisher;

    public NewsPublishedBusPublisher(IIntegrationEventPublisher publisher)
        => _publisher = publisher;

    public Task Handle(NewsPublishedEvent notification, CancellationToken cancellationToken)
        => _publisher.PublishAsync(new NewsPublishedIntegrationEvent(
            notification.NewsId,
            notification.TopicId,
            notification.AuthorId,
            notification.OccurredOn), cancellationToken);
}
