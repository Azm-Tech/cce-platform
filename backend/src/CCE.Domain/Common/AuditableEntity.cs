namespace CCE.Domain.Common;

/// <summary>
/// Base class for entities that expose generic audit timestamps.
/// Concrete entities call <see cref="MarkAsCreated"/> and <see cref="MarkAsModified"/>
/// from their own factory methods and mutators.
/// </summary>
/// <typeparam name="TId">The ID type.</typeparam>
public abstract class AuditableEntity<TId> : Entity<TId>, IAuditable
    where TId : notnull
{
    protected AuditableEntity(TId id) : base(id) { }

    /// <inheritdoc />
    public DateTimeOffset CreatedOn { get; protected set; }

    /// <inheritdoc />
    public Guid CreatedById { get; protected set; }

    /// <inheritdoc />
    public DateTimeOffset? LastModifiedOn { get; protected set; }

    /// <inheritdoc />
    public Guid? LastModifiedById { get; protected set; }

    /// <summary>Records creation metadata. Call from factory methods.</summary>
    protected void MarkAsCreated(Guid by, ISystemClock clock)
    {
        if (by == Guid.Empty) throw new DomainException("CreatedById is required.");
        CreatedOn = clock.UtcNow;
        CreatedById = by;
    }

    /// <summary>Records modification metadata. Call from mutator methods.</summary>
    protected void MarkAsModified(Guid by, ISystemClock clock)
    {
        if (by == Guid.Empty) throw new DomainException("ModifiedById is required.");
        LastModifiedOn = clock.UtcNow;
        LastModifiedById = by;
    }
}
