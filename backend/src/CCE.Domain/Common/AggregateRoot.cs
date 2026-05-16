namespace CCE.Domain.Common;

/// <summary>
/// Base class for DDD aggregate roots — entities that serve as the consistency boundary
/// for a cluster of related entities and value objects. Repositories are per-aggregate.
/// Inherits <see cref="SoftDeletableEntity{TId}"/> so every aggregate root automatically
/// supports audit timestamps and soft delete.
/// </summary>
/// <typeparam name="TId">The aggregate root's ID type.</typeparam>
public abstract class AggregateRoot<TId> : SoftDeletableEntity<TId>
    where TId : IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id) { }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
