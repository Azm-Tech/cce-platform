namespace CCE.Application.Common.Realtime;

/// <summary>
/// Coalesces ephemeral "user X is typing on post Y" events server-side so a long keystroke
/// burst doesn't saturate the WebSocket fanout. Implementations must be thread-safe; the
/// in-memory default is per-process (acceptable for a UX signal — see the plan's caveat).
/// A Redis-backed implementation can provide stricter cross-instance dedup using SETEX.
/// </summary>
public interface ITypingThrottle
{
    /// <summary>Returns true if the typing event should be broadcast (not throttled).</summary>
    bool ShouldBroadcast(System.Guid postId, System.Guid userId);
}