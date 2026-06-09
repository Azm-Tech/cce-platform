using CCE.Application.Common;
using CCE.Application.Common.Caching;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Cache;

/// <summary>
/// Scan Redis keys matching a pattern and return their names, types, and (for string keys) values.
/// Used by the admin diagnostics endpoint.
/// </summary>
public sealed record ListRedisKeysQuery(
    string Pattern = "*",
    int Count = 100) : IRequest<Response<IReadOnlyList<RedisKeyInfo>>>;

/// <summary>Metadata for a single Redis key returned by <see cref="ListRedisKeysQuery"/>.</summary>
public sealed record RedisKeyInfo(
    string Key,
    string Type,
    string? Value);

public sealed class ListRedisKeysQueryHandler
    : IRequestHandler<ListRedisKeysQuery, Response<IReadOnlyList<RedisKeyInfo>>>
{
    private readonly IRedisKeyInspector _inspector;
    private readonly MessageFactory _messages;

    public ListRedisKeysQueryHandler(IRedisKeyInspector inspector, MessageFactory messages)
    {
        _inspector = inspector;
        _messages = messages;
    }

    public async Task<Response<IReadOnlyList<RedisKeyInfo>>> Handle(
        ListRedisKeysQuery request, CancellationToken cancellationToken)
    {
        const int maxCount = 500;
        var count = request.Count <= 0 ? 100 : Math.Min(request.Count, maxCount);

        var keys = await _inspector.ListKeysAsync(request.Pattern, count, cancellationToken).ConfigureAwait(false);
        var infos = new List<RedisKeyInfo>(keys.Count);

        foreach (var key in keys)
        {
            var type = await _inspector.GetKeyTypeAsync(key, cancellationToken).ConfigureAwait(false);
            string? value = null;
            if (type == "string")
            {
                value = await _inspector.GetValueAsync(key, cancellationToken).ConfigureAwait(false);
            }
            infos.Add(new RedisKeyInfo(key, type, value));
        }

        return _messages.Ok<IReadOnlyList<RedisKeyInfo>>(infos, "ITEMS_LISTED");
    }
}
