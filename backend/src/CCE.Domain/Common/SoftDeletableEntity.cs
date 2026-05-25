namespace CCE.Domain.Common;

/// <summary>
/// Base class for entities that support soft delete and audit timestamps.
/// Inherits <see cref="AuditableEntity{TId}"/> and absorbs <see cref="ISoftDeletable"/>
/// so concrete entities do not copy-paste the same soft-delete implementation.
/// </summary>
/// <typeparam name="TId">The ID type.</typeparam>
public abstract class SoftDeletableEntity<TId> : AuditableEntity<TId>, ISoftDeletable
    where TId : IEquatable<TId>
{
    protected SoftDeletableEntity(TId id) : base(id) { }

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
        MarkAsModified(by, clock);
    }

    /// <summary>
    /// Restores a soft-deleted entity. Clears delete fields and records the restoration as a modification.
    /// </summary>
    public void Restore(Guid by, ISystemClock clock)
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedById = null;
        DeletedOn = null;
        MarkAsModified(by, clock);
    }
}
