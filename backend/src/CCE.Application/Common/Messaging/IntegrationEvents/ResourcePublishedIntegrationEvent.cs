namespace CCE.Application.Common.Messaging.IntegrationEvents;

/// <summary>
/// Scaffolding integration-event contract demonstrating the pattern: a plain immutable record with no
/// MassTransit attributes, carried over the bus via <see cref="IIntegrationEventPublisher"/>.
///
/// <para>
/// This is intentionally illustrative — no existing handler is force-migrated onto it. Domain-event
/// notifications already ride the bus durably through
/// <see cref="CCE.Application.Notifications.Messages.INotificationMessageDispatcher"/>. Add real
/// contracts here when a genuine cross-process consumer appears, and publish them from the relevant
/// MediatR domain-event handler.
/// </para>
/// </summary>
/// <param name="ResourceId">The published resource.</param>
/// <param name="CountryId">Owning country, when the resource is country-scoped.</param>
/// <param name="CategoryId">Resource category.</param>
/// <param name="OccurredOn">When the publish happened (carried from the domain event).</param>
public sealed record ResourcePublishedIntegrationEvent(
    System.Guid ResourceId,
    System.Guid? CountryId,
    System.Guid CategoryId,
    System.DateTimeOffset OccurredOn);
