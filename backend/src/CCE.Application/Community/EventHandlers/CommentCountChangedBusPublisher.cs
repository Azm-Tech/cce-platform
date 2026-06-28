using CCE.Application.Common.Messaging;
using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Domain.Community.Events;
using MediatR;

namespace CCE.Application.Community.EventHandlers;

/// <summary>
/// Bridge: translates the <see cref="CommentCountChangedEvent"/> domain event into a
/// <see cref="CommentCountChangedIntegrationEvent"/> on the bus. Captured by the MassTransit EF
/// outbox and committed atomically with the reply row. The Worker's <c>ReplyCountConsumer</c>
/// updates <c>post:{id}:meta.replyCount</c> in Redis so query handlers serve accurate comment counts.
/// </summary>
public sealed class CommentCountChangedBusPublisher : INotificationHandler<CommentCountChangedEvent>
{
    private readonly IIntegrationEventPublisher _publisher;

    public CommentCountChangedBusPublisher(IIntegrationEventPublisher publisher)
        => _publisher = publisher;

    public Task Handle(CommentCountChangedEvent notification, CancellationToken cancellationToken)
        => _publisher.PublishAsync(
            new CommentCountChangedIntegrationEvent(notification.PostId, notification.CommentsCount),
            cancellationToken);
}
