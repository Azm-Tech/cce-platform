using CCE.Application.Common;
using CCE.Application.Common.Caching;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Cache;

public sealed class EvictCacheKeyCommandHandler
    : IRequestHandler<EvictCacheKeyCommand, Response<VoidData>>
{
    private readonly IOutputCacheInvalidator _cache;
    private readonly MessageFactory _messages;

    public EvictCacheKeyCommandHandler(IOutputCacheInvalidator cache, MessageFactory messages)
    {
        _cache = cache;
        _messages = messages;
    }

    public async Task<Response<VoidData>> Handle(
        EvictCacheKeyCommand request, CancellationToken cancellationToken)
    {
        await _cache.EvictKeyAsync(request.Key, cancellationToken).ConfigureAwait(false);
        return _messages.Ok(MessageKeys.General.SUCCESS_DELETED);
    }
}
