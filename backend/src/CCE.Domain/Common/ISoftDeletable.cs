namespace CCE.Domain.Common;

/// <summary>
/// Marker interface for entities that support soft delete. Implementations expose
/// <see cref="IsDeleted"/>, <see cref="DeletedOn"/>, and <see cref="DeletedById"/>
/// and can be soft-deleted via <see cref="SoftDelete"/>.
/// </summary>
/// <remarks>
/// EF Core's <c>OnModelCreating</c> registers a global query filter
/// <c>HasQueryFilter(e =&gt; !e.IsDeleted)</c> for every entity type implementing this interface.
/// To bypass the filter (admin recovery flows, audit export), use <c>IgnoreQueryFilters()</c>.
/// </remarks>
public interface ISoftDeletable
{
    /// <summary>Whether this entity is soft-deleted.</summary>
    bool IsDeleted { get; }

    /// <summary>UTC moment the entity was soft-deleted; null when not deleted.</summary>
    DateTimeOffset? DeletedOn { get; }

    /// <summary>Identifier of the user/system that performed the soft delete; null when not deleted.</summary>
    Guid? DeletedById { get; }

    /// <summary>
    /// Marks this entity as soft-deleted. Idempotent — no-op if already deleted.
    /// </summary>
    /// <param name="by">Actor performing the deletion.</param>
    /// <param name="clock">Domain clock abstraction.</param>
    void SoftDelete(Guid by, ISystemClock clock);
}
