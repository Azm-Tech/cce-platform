using CCE.Application.Common;
using CCE.Application.Common.Caching;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Cache;

public sealed class EvictCacheRegionCommandHandler
    : IRequestHandler<EvictCacheRegionCommand, Response<VoidData>>
{
    private readonly IOutputCacheInvalidator _cache;
    private readonly MessageFactory _messages;

    public EvictCacheRegionCommandHandler(IOutputCacheInvalidator cache, MessageFactory messages)
    {
        _cache = cache;
        _messages = messages;
    }

    public async Task<Response<VoidData>> Handle(
        EvictCacheRegionCommand request, CancellationToken cancellationToken)
    {
        await _cache.EvictRegionsAsync([request.Region], cancellationToken).ConfigureAwait(false);
        return _messages.Ok("SUCCESS_OPERATION");
    }
}
