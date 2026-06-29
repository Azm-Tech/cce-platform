using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Application.Common.Realtime;
using CCE.Application.Community;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

/// <summary>
/// Pushes a live alert to the moderator SignalR room whenever the AI moderation pipeline
/// flags or rejects content (<see cref="ContentFlaggedIntegrationEvent"/>).
///
/// <para>
/// The durable record already exists in <c>moderation_record</c> — admins can always pull it from
/// the moderation queue endpoint. This consumer is the push half: connected moderators see flagged
/// content immediately instead of polling. Best-effort (the realtime publisher swallows transport
/// failures), so a missed push never blocks the pipeline.
/// </para>
/// </summary>
public sealed class ContentFlaggedNotificationConsumer : IConsumer<ContentFlaggedIntegrationEvent>
{
    private readonly ICommunityRealtimePublisher _realtime;
    private readonly ILogger<ContentFlaggedNotificationConsumer> _logger;

    public ContentFlaggedNotificationConsumer(
        ICommunityRealtimePublisher realtime,
        ILogger<ContentFlaggedNotificationConsumer> logger)
    {
        _realtime = realtime;
        _logger   = logger;
    }

    public async Task Consume(ConsumeContext<ContentFlaggedIntegrationEvent> context)
    {
        var evt = context.Message;

        await _realtime.PublishToModeratorsAsync(
            RealtimeEvents.ContentFlagged,
            new ContentFlaggedRealtime(
                evt.ContentType, evt.ContentId, evt.Status.ToString(), evt.Category, evt.Reason),
            context.CancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "ContentFlaggedNotificationConsumer: alerted moderators about {ContentType} {ContentId} ({Status})",
            evt.ContentType, evt.ContentId, evt.Status);
    }
}
