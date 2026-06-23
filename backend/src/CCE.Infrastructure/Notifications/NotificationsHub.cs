using CCE.Application.Common.Realtime;
using CCE.Application.Community;
using CCE.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications;

/// <summary>
/// Realtime hub. Rooms (see <see cref="RealtimeGroups"/>):
/// <list type="bullet">
///   <item><c>user:{id}</c> — auto-joined; personal notifications.</item>
///   <item><c>post:{id}</c> — joined via <see cref="Subscribe"/> (read-access checked); replies/votes/poll/presence/typing.</item>
///   <item><c>community:{id}</c> / <c>topic:{id}</c> — feed events.</item>
///   <item><c>moderation</c> — auto-joined by moderators; content-moderation events.</item>
/// </list>
/// Requires an authenticated connection; joins to post/community rooms are authorized via
/// <see cref="ICommunityAccessGuard"/> so private-community activity isn't leaked.
/// </summary>
[Authorize]
public sealed class NotificationsHub : Hub
{
    private const int MaxPostSubscriptionsPerConnection = 50;
    private const string PostSubCountKey = "postSubCount";

    private readonly IPostRepository _posts;
    private readonly ICommunityAccessGuard _access;
    private readonly IRealtimePresenceTracker _presence;
    private readonly IAuthorizationService _authorization;
    private readonly ITypingThrottle _typingThrottle;
    private readonly ILogger<NotificationsHub> _logger;

    public NotificationsHub(
        IPostRepository posts,
        ICommunityAccessGuard access,
        IRealtimePresenceTracker presence,
        IAuthorizationService authorization,
        ITypingThrottle typingThrottle,
        ILogger<NotificationsHub> logger)
    {
        _posts = posts;
        _access = access;
        _presence = presence;
        _authorization = authorization;
        _typingThrottle = typingThrottle;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrWhiteSpace(userId))
        {
            // Auth succeeded but no sub claim was extracted — SubClaimUserIdProvider is misconfigured
            // or the JWT issuance path lost the sub. The user gets no personal notifications and a
            // silent degraded experience. Warn loudly so ops catches it.
            _logger.LogWarning(
                "Authenticated connection {ConnectionId} has no UserIdentifier (sub claim missing). " +
                "Personal notifications and presence will not work for this connection.",
                Context.ConnectionId);
        }
        else
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.User(userId)).ConfigureAwait(false);
        }

        // Moderators also join the global moderation room.
        if (Context.User is not null)
        {
            var result = await _authorization.AuthorizeAsync(Context.User, Permissions.Community_Post_Moderate).ConfigureAwait(false);
            if (result.Succeeded)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.Moderation).ConfigureAwait(false);
            }
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <summary>Join a post's live room (VoteChanged / NewReply / PollResultsChanged / presence / typing).</summary>
    public async Task Subscribe(System.Guid postId)
    {
        var count = (int)(Context.Items[PostSubCountKey] ?? 0);
        if (count >= MaxPostSubscriptionsPerConnection)
            throw new HubException("Too many active post subscriptions.");

        var communityId = await _posts.GetCommunityIdAsync(postId, Context.ConnectionAborted).ConfigureAwait(false)
            ?? throw new HubException("Post not found.");
        await EnsureCanReadAsync(communityId).ConfigureAwait(false);

        await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.Post(postId)).ConfigureAwait(false);
        var viewers = await _presence.JoinAsync(postId, Context.UserIdentifier ?? string.Empty, Context.ConnectionId, Context.ConnectionAborted).ConfigureAwait(false);
        await BroadcastPresenceAsync(postId, viewers).ConfigureAwait(false);

        Context.Items[PostSubCountKey] = count + 1;
    }

    /// <summary>Leave a post's live room.</summary>
    public async Task Unsubscribe(System.Guid postId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroups.Post(postId)).ConfigureAwait(false);
        var viewers = await _presence.LeaveAsync(postId, Context.UserIdentifier ?? string.Empty, Context.ConnectionId, Context.ConnectionAborted).ConfigureAwait(false);
        await BroadcastPresenceAsync(postId, viewers).ConfigureAwait(false);

        if (Context.Items.TryGetValue(PostSubCountKey, out var box) && box is int c && c > 0)
            Context.Items[PostSubCountKey] = c - 1;
    }

    /// <summary>Join a community's feed room (NewPost / PostModerated). Read-access checked.</summary>
    public async Task SubscribeCommunity(System.Guid communityId)
    {
        await EnsureCanReadAsync(communityId).ConfigureAwait(false);
        await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.Community(communityId)).ConfigureAwait(false);
    }

    public Task UnsubscribeCommunity(System.Guid communityId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroups.Community(communityId));

    /// <summary>Join a topic's feed room (NewPost). Topics are public reads — authenticated is enough.</summary>
    public Task SubscribeTopic(System.Guid topicId)
        => Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.Topic(topicId));

    public Task UnsubscribeTopic(System.Guid topicId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroups.Topic(topicId));

    /// <summary>Broadcast a typing indicator to everyone else viewing the post.</summary>
    public Task StartTyping(System.Guid postId) => BroadcastTypingAsync(postId, isTyping: true);

    public Task StopTyping(System.Guid postId) => BroadcastTypingAsync(postId, isTyping: false);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Clear presence for every post this connection was viewing and notify those rooms.
        var changes = await _presence.LeaveAllAsync(Context.ConnectionId, System.Threading.CancellationToken.None).ConfigureAwait(false);
        foreach (var change in changes)
        {
            await BroadcastPresenceAsync(change.PostId, change.Viewers).ConfigureAwait(false);
        }

        var userId = Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroups.User(userId)).ConfigureAwait(false);
        }

        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    private async Task EnsureCanReadAsync(System.Guid communityId)
    {
        var userId = System.Guid.TryParse(Context.UserIdentifier, out var id) ? id : (System.Guid?)null;
        if (!await _access.CanReadAsync(communityId, userId, Context.ConnectionAborted).ConfigureAwait(false))
        {
            throw new HubException("Access denied.");
        }
    }

    private Task BroadcastPresenceAsync(System.Guid postId, int viewers)
        => Clients.Group(RealtimeGroups.Post(postId))
            .SendAsync(RealtimeEvents.PresenceChanged,
                RealtimeEnvelope.Wrap(new PresenceChangedRealtime(postId, viewers)));

    private Task BroadcastTypingAsync(System.Guid postId, bool isTyping)
    {
        if (!System.Guid.TryParse(Context.UserIdentifier, out var userId))
            return Task.CompletedTask;

        // Only throttle "started typing" — always let "stopped" through so the indicator
        // clears promptly. See ITypingThrottle for the cross-instance caveat.
        if (isTyping && !_typingThrottle.ShouldBroadcast(postId, userId))
            return Task.CompletedTask;

        return Clients.OthersInGroup(RealtimeGroups.Post(postId))
            .SendAsync(RealtimeEvents.TypingChanged,
                RealtimeEnvelope.Wrap(new TypingChangedRealtime(postId, userId, isTyping)));
    }
}
