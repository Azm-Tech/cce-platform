using CCE.Application.Common.Messaging;
using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Domain.Community.Events;
using MediatR;

namespace CCE.Application.Notifications.Handlers;

/// <summary>
/// Bridge: translates the <see cref="PostVotedEvent"/> domain event into a
/// <see cref="VoteCreatedIntegrationEvent"/> on the bus. Runs pre-commit inside
/// <c>DomainEventDispatcher</c>, so the publish is captured by the MassTransit EF outbox and
/// committed atomically with the vote. The Worker's VoteConsumer then updates Redis hot counters.
/// </summary>
public sealed class PostVotedBusPublisher : INotificationHandler<PostVotedEvent>
{
    private readonly IIntegrationEventPublisher _publisher;

    public PostVotedBusPublisher(IIntegrationEventPublisher publisher)
        => _publisher = publisher;

    public Task Handle(PostVotedEvent notification, CancellationToken cancellationToken)
        => _publisher.PublishAsync(new VoteCreatedIntegrationEvent(
            notification.PostId,
            notification.UserId,
            notification.Direction,
            notification.UpvoteCount,
            notification.DownvoteCount,
            notification.Score), cancellationToken);
}
