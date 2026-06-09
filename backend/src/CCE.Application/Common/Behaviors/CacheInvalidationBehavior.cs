using CCE.Application.Common.Caching;
using MediatR;

namespace CCE.Application.Common.Behaviors;

/// <summary>
/// After a request that implements <see cref="ICacheInvalidatingRequest"/> completes <em>successfully</em>,
/// purges the cache regions it declares. Runs <em>after</em> <c>next()</c> — i.e. after the handler has
/// committed — so the cache reflects committed state and reads repopulate fresh.
///
/// <para>Deliberately separate from the (now pre-commit) domain-event dispatch: evicting before commit
/// could let a concurrent read repopulate stale data.</para>
/// </summary>
public sealed class CacheInvalidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IOutputCacheInvalidator _invalidator;

    public CacheInvalidationBehavior(IOutputCacheInvalidator invalidator)
        => _invalidator = invalidator;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next().ConfigureAwait(false);

        if (request is ICacheInvalidatingRequest invalidating
            && invalidating.CacheRegionsToEvict.Count > 0
            && response is IResponse { Success: true })
        {
            await _invalidator
                .EvictRegionsAsync(invalidating.CacheRegionsToEvict, cancellationToken)
                .ConfigureAwait(false);
        }

        return response;
    }
}
