using System.Collections.Generic;
using System.Linq;
using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Application.Community;
using CCE.Application.Notifications;
using CCE.Application.Notifications.Messages;
using CCE.Application.Common.Interfaces;
using CCE.Domain.Notifications;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

/// <summary>
/// Consumes <see cref="PostCreatedIntegrationEvent"/>, <see cref="ReplyCreatedIntegrationEvent"/> and
/// <see cref="CommunityJoinRequestedIntegrationEvent"/> from the bus and dispatches
/// <see cref="NotificationMessage"/> instances to the relevant recipients. All notification fan-out
/// runs here in the Worker so the API thread returns immediately (the post follower fan-out used to run
/// synchronously in the API request).
/// </summary>
public sealed class NotificationConsumer :
    IConsumer<PostCreatedIntegrationEvent>,
    IConsumer<ReplyCreatedIntegrationEvent>,
    IConsumer<CommunityJoinRequestedIntegrationEvent>
{
    private readonly ICceDbContext _db;
    private readonly ICommunityReadService _communityRead;
    private readonly INotificationMessageDispatcher _dispatcher;
    private readonly ILogger<NotificationConsumer> _logger;

    public NotificationConsumer(
        ICceDbContext db, ICommunityReadService communityRead,
        INotificationMessageDispatcher dispatcher, ILogger<NotificationConsumer> logger)
    {
        _db = db;
        _communityRead = communityRead;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PostCreatedIntegrationEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation(
            "NotificationConsumer: PostCreated PostId={PostId} Community={CommunityId} Topic={TopicId}",
            evt.PostId, evt.CommunityId, evt.TopicId);

        // Notify topic + community followers (excluding the author), unioned so a user following both is
        // notified once. Heavy fan-out query now runs in the Worker, not the API request thread.
        var topicFollowers = await _communityRead
            .GetTopicFollowerIdsAsync(evt.TopicId, evt.AuthorId, context.CancellationToken).ConfigureAwait(false);
        var communityFollowers = await _communityRead
            .GetCommunityFollowerIdsAsync(evt.CommunityId, evt.AuthorId, context.CancellationToken).ConfigureAwait(false);

        var recipientIds = new HashSet<Guid>(topicFollowers);
        recipientIds.UnionWith(communityFollowers);

        foreach (var userId in recipientIds)
        {
            await _dispatcher.DispatchAsync(new NotificationMessage(
                TemplateCode: "COMMUNITY_POST_CREATED",
                RecipientUserId: userId,
                EventType: NotificationEventType.CommunityPostCreated,
                Channels: [NotificationChannel.InApp],
                MetaData: new Dictionary<string, string> { ["postId"] = evt.PostId.ToString() },
                Locale: evt.Locale), context.CancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "NotificationConsumer: Dispatched {Count} notifications for PostId={PostId}.",
            recipientIds.Count, evt.PostId);
    }

    public async Task Consume(ConsumeContext<ReplyCreatedIntegrationEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation(
            "NotificationConsumer: ReplyCreated ReplyId={ReplyId} PostId={PostId} Author={AuthorId}",
            evt.ReplyId, evt.PostId, evt.AuthorId);

        // Recipients: post followers + post author + parent-reply author (if nested).
        var recipientIds = new HashSet<Guid>();

        var postFollowers = await _db.PostFollows
            .AsNoTracking()
            .Where(f => f.PostId == evt.PostId)
            .Select(f => f.UserId)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        recipientIds.UnionWith(postFollowers);

        var postAuthor = await _db.Posts
            .AsNoTracking()
            .Where(p => p.Id == evt.PostId)
            .Select(p => p.AuthorId)
            .FirstOrDefaultAsync(context.CancellationToken)
            .ConfigureAwait(false);
        if (postAuthor != default) recipientIds.Add(postAuthor);

        if (evt.ParentReplyId.HasValue)
        {
            var parentAuthor = await _db.PostReplies
                .AsNoTracking()
                .Where(r => r.Id == evt.ParentReplyId.Value)
                .Select(r => r.AuthorId)
                .FirstOrDefaultAsync(context.CancellationToken)
                .ConfigureAwait(false);
            if (parentAuthor != default) recipientIds.Add(parentAuthor);
        }

        // Exclude the reply author (don't self-notify).
        recipientIds.Remove(evt.AuthorId);

        foreach (var userId in recipientIds)
        {
            await _dispatcher.DispatchAsync(new NotificationMessage(
                TemplateCode: "POST_REPLIED",
                RecipientUserId: userId,
                EventType: NotificationEventType.CommunityPostReplied,
                Channels: [NotificationChannel.InApp],
                MetaData: new Dictionary<string, string>
                {
                    ["postId"] = evt.PostId.ToString(),
                    ["replyId"] = evt.ReplyId.ToString(),
                },
                Locale: "en"), context.CancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "NotificationConsumer: Dispatched {Count} notifications for ReplyId={ReplyId}.",
            recipientIds.Count, evt.ReplyId);
    }

    public async Task Consume(ConsumeContext<CommunityJoinRequestedIntegrationEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation(
            "NotificationConsumer: JoinRequested RequestId={RequestId} CommunityId={CommunityId} UserId={UserId}",
            evt.RequestId, evt.CommunityId, evt.UserId);

        // Notify community moderators.
        var moderatorIds = await _db.CommunityMemberships
            .AsNoTracking()
            .Where(m => m.CommunityId == evt.CommunityId && m.Role == Domain.Community.CommunityRole.Moderator)
            .Select(m => m.UserId)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);

        foreach (var modId in moderatorIds)
        {
            await _dispatcher.DispatchAsync(new NotificationMessage(
                TemplateCode: "COMMUNITY_JOIN_REQUESTED",
                RecipientUserId: modId,
                EventType: NotificationEventType.CommunityJoinRequested,
                Channels: [NotificationChannel.InApp],
                MetaData: new Dictionary<string, string>
                {
                    ["communityId"] = evt.CommunityId.ToString(),
                    ["requestId"] = evt.RequestId.ToString(),
                    ["userId"] = evt.UserId.ToString(),
                },
                Locale: "en"), context.CancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "NotificationConsumer: Dispatched {Count} moderator notifications for CommunityId={CommunityId}.",
            moderatorIds.Count, evt.CommunityId);
    }
}
