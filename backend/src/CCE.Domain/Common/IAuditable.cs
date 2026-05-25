namespace CCE.Domain.Common;

/// <summary>
/// Marker interface for entities that expose generic audit timestamps.
/// Domain-specific timestamps (e.g. <c>PublishedOn</c>, <c>SubmittedOn</c>)
/// belong on the concrete entity, not this interface.
/// </summary>
public interface IAuditable
{
    /// <summary>UTC moment this entity was created.</summary>
    DateTimeOffset CreatedOn { get; }

    /// <summary>Actor that created this entity.</summary>
    Guid CreatedById { get; }

    /// <summary>UTC moment this entity was last modified; null if never modified after creation.</summary>
    DateTimeOffset? LastModifiedOn { get; }

    /// <summary>Actor that last modified this entity; null if never modified after creation.</summary>
    Guid? LastModifiedById { get; }
}
