using Microsoft.AspNetCore.SignalR;

namespace CCE.Infrastructure.Notifications;

public sealed class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}").ConfigureAwait(false);
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <summary>Join a post's live group to receive VoteChanged / NewReply / PollResultsChanged events.</summary>
    public Task Subscribe(System.Guid postId)
        => Groups.AddToGroupAsync(Context.ConnectionId, $"post:{postId}");

    /// <summary>Leave a post's live group.</summary>
    public Task Unsubscribe(System.Guid postId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"post:{postId}");

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}").ConfigureAwait(false);
        }

        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}
