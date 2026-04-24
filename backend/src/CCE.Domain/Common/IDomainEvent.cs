namespace CCE.Domain.Common;

/// <summary>
/// Marker interface for domain events raised by aggregate roots.
/// Dispatched by the infrastructure layer post-persistence (see Phase 06).
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
