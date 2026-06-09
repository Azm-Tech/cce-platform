namespace CCE.Application.Common.Realtime;

/// <summary>
/// Tracks which users are currently viewing a post across all processes (presence). Best-effort and
/// backed by Redis so it works behind the SignalR backplane; a Redis outage degrades to "no presence".
/// Returns the distinct-user viewer count so the hub can broadcast <c>PresenceChanged</c>.
/// </summary>
public interface IRealtimePresenceTracker
{
    /// <summary>Record a connection viewing a post. Returns the new distinct-user viewer count.</summary>
    Task<int> JoinAsync(System.Guid postId, string userId, string connectionId, CancellationToken cancellationToken);

    /// <summary>Remove a connection from a post. Returns the new distinct-user viewer count.</summary>
    Task<int> LeaveAsync(System.Guid postId, string userId, string connectionId, CancellationToken cancellationToken);

    /// <summary>Remove a connection from every post it was viewing (on disconnect). Returns the affected posts + new counts.</summary>
    Task<IReadOnlyList<PresenceChange>> LeaveAllAsync(string connectionId, CancellationToken cancellationToken);
}

/// <summary>A post whose viewer count changed, with the new count.</summary>
public sealed record PresenceChange(System.Guid PostId, int Viewers);
