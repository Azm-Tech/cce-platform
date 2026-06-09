namespace CCE.Application.Common.Caching;

/// <summary>
/// Read-only inspector for raw Redis keys. Lists keys by pattern and surfaces their values/types.
/// Used by the admin diagnostics endpoints. All methods degrade gracefully (RedisException → empty result).
/// </summary>
public interface IRedisKeyInspector
{
    /// <summary>
    /// Scan the keyspace for keys matching <paramref name="pattern"/> (e.g. <c>CCE:*</c>).
    /// Returns at most <paramref name="count"/> keys.
    /// </summary>
    Task<IReadOnlyList<string>> ListKeysAsync(string pattern, int count, CancellationToken cancellationToken);

    /// <summary>
    /// Get the string value of a single key. Returns <c>null</c> if the key does not exist or is not a string.
    /// </summary>
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Get the Redis type of a key (e.g. <c>"string"</c>, <c>"hash"</c>, <c>"set"</c>, <c>"none"</c>).
    /// </summary>
    Task<string> GetKeyTypeAsync(string key, CancellationToken cancellationToken);
}
