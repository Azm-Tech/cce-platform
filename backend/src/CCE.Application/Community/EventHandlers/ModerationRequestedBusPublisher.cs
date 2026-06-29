using CCE.Application.Common.Messaging;
using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Domain.Community.Events;
using MediatR;

namespace CCE.Application.Community.EventHandlers;

/// <summary>
/// Bridge: when a post is published, queues it for async AI moderation by publishing
/// <see cref="ContentModerationRequestedIntegrationEvent"/> via the EF outbox — atomic
/// with the aggregate save. The moderation consumer runs ~1 s later in the Worker.
/// </summary>
public sealed class PostModerationRequestedBusPublisher : INotificationHandler<PostCreatedEvent>
{
    private readonly IIntegrationEventPublisher _publisher;

    public PostModerationRequestedBusPublisher(IIntegrationEventPublisher publisher)
        => _publisher = publisher;

    public Task Handle(PostCreatedEvent notification, CancellationToken cancellationToken)
        => _publisher.PublishAsync(new ContentModerationRequestedIntegrationEvent(
            notification.PostId,
            "post",
            notification.Content,
            notification.Locale), cancellationToken);
}

/// <summary>
/// Bridge: when a reply is created, queues it for async AI moderation. Sends the full
/// reply <see cref="ReplyCreatedEvent.Content"/> — not the 120-char snippet — so the
/// AI receives the complete text regardless of reply length.
/// </summary>
public sealed class ReplyModerationRequestedBusPublisher : INotificationHandler<ReplyCreatedEvent>
{
    private readonly IIntegrationEventPublisher _publisher;

    public ReplyModerationRequestedBusPublisher(IIntegrationEventPublisher publisher)
        => _publisher = publisher;

    public Task Handle(ReplyCreatedEvent notification, CancellationToken cancellationToken)
        => _publisher.PublishAsync(new ContentModerationRequestedIntegrationEvent(
            notification.ReplyId,
            "reply",
            notification.Content,
            string.Empty), cancellationToken);
}
