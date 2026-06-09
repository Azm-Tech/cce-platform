namespace CCE.Application.Common.Messaging;

/// <summary>
/// Publishes an <em>integration event</em> — a cross-process, fire-and-forget message — onto the
/// message bus. This is the single abstraction Application-layer code uses for asynchronous,
/// out-of-band work; the concrete implementation (MassTransit + the transactional outbox) lives in
/// the Infrastructure layer, so nothing here takes a dependency on MassTransit.
///
/// <para>
/// Mirrors <see cref="CCE.Application.Notifications.Messages.INotificationMessageDispatcher"/>, which
/// is the notification-specific specialisation of the same idea. New cross-service events should use
/// this general publisher with a contract from <c>CCE.Application.Common.Messaging.IntegrationEvents</c>.
/// </para>
///
/// <para>
/// When the bus outbox is active, the call is captured into the <c>outbox_message</c> table inside the
/// caller's current <c>CceDbContext</c> transaction and relayed to the broker after
/// <c>SaveChanges</c> commits — so publishing is atomic with the aggregate change that triggered it.
/// </para>
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>Publish <paramref name="event"/> to the bus (captured by the outbox when enabled).</summary>
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken)
        where T : class;
}
