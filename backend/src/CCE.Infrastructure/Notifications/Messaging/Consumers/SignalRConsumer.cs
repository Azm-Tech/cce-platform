using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Application.Common.Realtime;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

/// <summary>
/// Consumes <see cref="PostCreatedIntegrationEvent"/> from the bus and pushes real-time
/// <c>NewPost</c> events to the <c>community:{communityId}</c> and <c>topic:{topicId}</c>
/// SignalR groups via the Redis backplane. This keeps the API publish-only; the Worker owns
/// all cross-process SignalR pushes.
/// </summary>
public sealed class SignalRConsumer : IConsumer<PostCreatedIntegrationEvent>
{
    private readonly IHubContext<NotificationsHub> _hub;
    private readonly ILogger<SignalRConsumer> _logger;

    public SignalRConsumer(IHubContext<NotificationsHub> hub, ILogger<SignalRConsumer> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PostCreatedIntegrationEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation(
            "SignalRConsumer: PostCreated PostId={PostId} Community={CommunityId} Topic={TopicId}",
            evt.PostId, evt.CommunityId, evt.TopicId);

        var payload = new
        {
            evt.PostId,
            evt.CommunityId,
            evt.TopicId,
            evt.AuthorId,
            evt.PublishedOn,
        };

        await _hub.Clients
            .Group(RealtimeGroups.Community(evt.CommunityId))
            .SendAsync(RealtimeEvents.NewPost, payload, context.CancellationToken)
            .ConfigureAwait(false);

        await _hub.Clients
            .Group(RealtimeGroups.Topic(evt.TopicId))
            .SendAsync(RealtimeEvents.NewPost, payload, context.CancellationToken)
            .ConfigureAwait(false);
    }
}
