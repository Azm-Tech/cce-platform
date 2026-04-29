namespace CCE.Domain.Common;

/// <summary>
/// Marker interface for domain events raised by aggregate roots.
/// Extends <see cref="MediatR.INotification"/> so that
/// <see cref="MediatR.INotificationHandler{TNotification}"/> implementations in
/// CCE.Infrastructure can receive events dispatched post-persistence.
/// Dispatched by the infrastructure layer post-persistence (see Phase 06).
/// </summary>
public interface IDomainEvent : MediatR.INotification
{
    DateTimeOffset OccurredOn { get; }
}
