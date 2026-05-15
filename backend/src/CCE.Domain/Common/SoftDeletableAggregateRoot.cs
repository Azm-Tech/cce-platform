namespace CCE.Domain.Common;

/// <summary>
/// Base class for DDD aggregate roots that support soft delete and audit timestamps.
/// Inherits <see cref="AuditableAggregateRoot{TId}"/> and absorbs <see cref="ISoftDeletable"/>
/// so concrete aggregates do not copy-paste the same soft-delete implementation.
/// </summary>
/// <typeparam name="TId">The aggregate root's ID type.</typeparam>
public abstract class SoftDeletableAggregateRoot<TId> : AuditableAggregateRoot<TId>, ISoftDeletable
    where TId : notnull
{
    protected SoftDeletableAggregateRoot(TId id) : base(id) { }

    /// <inheritdoc />
    public bool IsDeleted { get; protected set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedOn { get; protected set; }

    /// <inheritdoc />
    public Guid? DeletedById { get; protected set; }

    /// <inheritdoc />
    public void SoftDelete(Guid by, ISystemClock clock)
    {
        if (by == Guid.Empty) throw new DomainException("DeletedById is required.");
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedById = by;
        DeletedOn = clock.UtcNow;
    }
}
