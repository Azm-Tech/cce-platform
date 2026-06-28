using CCE.Application.Common;
using CCE.Application.Common.Caching;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Cache;

public sealed class GetCacheRegionsQueryHandler
    : IRequestHandler<GetCacheRegionsQuery, Response<IReadOnlyList<CacheRegionStatus>>>
{
    private readonly IOutputCacheInvalidator _cache;
    private readonly MessageFactory _messages;

    public GetCacheRegionsQueryHandler(IOutputCacheInvalidator cache, MessageFactory messages)
    {
        _cache = cache;
        _messages = messages;
    }

    public async Task<Response<IReadOnlyList<CacheRegionStatus>>> Handle(
        GetCacheRegionsQuery request, CancellationToken cancellationToken)
    {
        var status = await _cache.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        return _messages.Ok(status, MessageKeys.General.ITEMS_LISTED);
    }
}
