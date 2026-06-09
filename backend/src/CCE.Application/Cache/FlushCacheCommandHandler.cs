using CCE.Application.Common;
using CCE.Application.Common.Caching;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Cache;

public sealed class FlushCacheCommandHandler
    : IRequestHandler<FlushCacheCommand, Response<VoidData>>
{
    private readonly IOutputCacheInvalidator _cache;
    private readonly MessageFactory _messages;

    public FlushCacheCommandHandler(IOutputCacheInvalidator cache, MessageFactory messages)
    {
        _cache = cache;
        _messages = messages;
    }

    public async Task<Response<VoidData>> Handle(
        FlushCacheCommand request, CancellationToken cancellationToken)
    {
        await _cache.FlushAllAsync(cancellationToken).ConfigureAwait(false);
        return _messages.Ok("SUCCESS_OPERATION");
    }
}
