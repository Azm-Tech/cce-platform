using CCE.Application.Common.Messaging;
using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Domain.Community.Events;
using MediatR;

namespace CCE.Application.Notifications.Handlers;

/// <summary>
/// Bridge: translates the <see cref="ReplyCreatedEvent"/> domain event into a
/// <see cref="ReplyCreatedIntegrationEvent"/> on the bus. Runs pre-commit inside
/// <c>DomainEventDispatcher</c>, so the publish is captured by the MassTransit EF outbox and
/// committed atomically with the reply. The Worker's NotificationConsumer fans out notifications.
/// </summary>
public sealed class ReplyCreatedBusPublisher : INotificationHandler<ReplyCreatedEvent>
{
    private readonly IIntegrationEventPublisher _publisher;

    public ReplyCreatedBusPublisher(IIntegrationEventPublisher publisher)
        => _publisher = publisher;

    public Task Handle(ReplyCreatedEvent notification, CancellationToken cancellationToken)
        => _publisher.PublishAsync(new ReplyCreatedIntegrationEvent(
            notification.ReplyId,
            notification.PostId,
            notification.ParentReplyId,
            notification.AuthorId,
            notification.ContentSnippet,
            notification.OccurredOn), cancellationToken);
}
