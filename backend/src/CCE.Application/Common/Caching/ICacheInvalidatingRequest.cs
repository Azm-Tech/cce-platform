namespace CCE.Application.Common.Caching;

/// <summary>
/// Marker for a MediatR request (typically a write command) whose successful execution should purge one
/// or more output-cache regions. The <see cref="Behaviors.CacheInvalidationBehavior{TRequest,TResponse}"/>
/// reads <see cref="CacheRegionsToEvict"/> and evicts those regions <em>after</em> the handler completes
/// (post-commit), so reads repopulate from fresh data.
/// </summary>
public interface ICacheInvalidatingRequest
{
    /// <summary>Region names (see <see cref="CacheRegions"/>) to purge on success.</summary>
    IReadOnlyCollection<string> CacheRegionsToEvict { get; }
}
