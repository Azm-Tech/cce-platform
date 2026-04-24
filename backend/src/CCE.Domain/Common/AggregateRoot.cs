namespace CCE.Domain.Common;

/// <summary>
/// Base class for DDD aggregate roots — entities that serve as the consistency boundary
/// for a cluster of related entities and value objects. Repositories are per-aggregate.
/// </summary>
/// <typeparam name="TId">The aggregate root's ID type.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    protected AggregateRoot(TId id) : base(id) { }
}
