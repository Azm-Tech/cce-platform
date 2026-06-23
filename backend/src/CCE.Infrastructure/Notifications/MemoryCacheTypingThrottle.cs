using System;
using CCE.Application.Common.Realtime;
using Microsoft.Extensions.Caching.Memory;

namespace CCE.Infrastructure.Notifications;

/// <summary>
/// Per-process typing debounce using <see cref="IMemoryCache"/>. Coalesces "started typing"
/// events to one per 2 s per (post, user) pair; "stopped typing" is never throttled so the
/// indicator always clears promptly.
///
/// <para>
/// <b>Multi-instance caveat:</b> the cache is per-process. With the External + Internal APIs on
/// separate hosts sharing the Redis SignalR backplane, a single user could emit one
/// <c>TypingChanged</c> per instance per 2 s window (i.e. up to 2× across the fleet). Acceptable
/// for an ephemeral UX signal. If stricter de-dup is ever needed, replace with a Redis
/// <c>SETEX typing:{postId}:{userId} 2 NX</c> check in <see cref="ShouldBroadcast"/> (reuses the
/// existing <c>IConnectionMultiplexer</c>).
/// </para>
/// </summary>
public sealed class MemoryCacheTypingThrottle : ITypingThrottle
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(2);
    private readonly IMemoryCache _cache;

    public MemoryCacheTypingThrottle(IMemoryCache cache) => _cache = cache;

    public bool ShouldBroadcast(Guid postId, Guid userId)
    {
        var key = $"typing:{postId}:{userId}";
        if (_cache.TryGetValue(key, out _)) return false;
        _cache.Set(key, true, Window);
        return true;
    }
}