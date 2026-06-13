using CCE.Application.Common.Messaging;
using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Domain.Community.Events;
using MediatR;

namespace CCE.Application.Community.EventHandlers;

/// <summary>
/// Bridge: when <see cref="PostCreatedEvent"/> is dispatched (pre-commit, inside the
/// same <c>SaveChanges</c> transaction), publishes <see cref="PostCreatedIntegrationEvent"/>
/// onto the bus via <see cref="IIntegrationEventPublisher"/>.
///
/// <para>
/// Because this handler runs inside <c>SavingChangesAsync</c> (see
/// <c>DomainEventDispatcher</c>), the integration-event publish is captured by MassTransit's
/// EF outbox and committed atomically with the aggregate. The Worker then relays it to
/// RabbitMQ for cross-process consumers (feed fan-out, ranking, SignalR push).
/// </para>
/// </summary>
public sealed class PostCreatedBusPublisher : INotificationHandler<PostCreatedEvent>
{
    private readonly IIntegrationEventPublisher _publisher;

    public PostCreatedBusPublisher(IIntegrationEventPublisher publisher)
        => _publisher = publisher;

    public Task Handle(PostCreatedEvent notification, CancellationToken cancellationToken)
    {
        var evt = new PostCreatedIntegrationEvent(
            notification.PostId,
            notification.CommunityId,
            notification.TopicId,
            notification.AuthorId,
            notification.OccurredOn,
            IsExpert: false, // Worker will resolve IsExpert from ExpertProfile if needed
            notification.Locale);

        return _publisher.PublishAsync(evt, cancellationToken);
    }
}
