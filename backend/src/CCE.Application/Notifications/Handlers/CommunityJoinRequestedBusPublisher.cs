using CCE.Application.Common.Messaging;
using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Domain.Community.Events;
using MediatR;

namespace CCE.Application.Notifications.Handlers;

/// <summary>
/// Bridge: translates the <see cref="CommunityJoinRequestedEvent"/> domain event into a
/// <see cref="CommunityJoinRequestedIntegrationEvent"/> on the bus. Runs pre-commit inside
/// <c>DomainEventDispatcher</c>, so the publish is captured by the MassTransit EF outbox and
/// committed atomically with the join request. The Worker's NotificationConsumer notifies moderators.
/// </summary>
public sealed class CommunityJoinRequestedBusPublisher : INotificationHandler<CommunityJoinRequestedEvent>
{
    private readonly IIntegrationEventPublisher _publisher;

    public CommunityJoinRequestedBusPublisher(IIntegrationEventPublisher publisher)
        => _publisher = publisher;

    public Task Handle(CommunityJoinRequestedEvent notification, CancellationToken cancellationToken)
        => _publisher.PublishAsync(new CommunityJoinRequestedIntegrationEvent(
            notification.RequestId,
            notification.CommunityId,
            notification.UserId,
            notification.OccurredOn), cancellationToken);
}
