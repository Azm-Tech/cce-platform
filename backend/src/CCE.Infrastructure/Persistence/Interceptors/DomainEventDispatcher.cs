using CCE.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CCE.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Spec §3.5 + §5.4. <strong>Pre-commit</strong> interceptor that drains <see cref="IDomainEvent"/>s from
/// every aggregate root tracked by the context and publishes them via <see cref="IPublisher"/> (MediatR)
/// <em>before</em> the changes are written.
///
/// <para>
/// Dispatch runs in <see cref="SavingChangesAsync"/> (not <c>SavedChangesAsync</c>) so that any bus
/// publishes performed by the in-process handlers are captured by the MassTransit EF bus outbox into the
/// <c>outbox_message</c> table and persisted by the <em>same</em> <c>SaveChanges</c> as the aggregate —
/// making async event delivery atomic with the state change (no dual-write / lost-message window). Adding
/// the outbox rows during this interceptor is safe: EF includes entities added in <c>SavingChangesAsync</c>
/// in the in-flight save, and the notification handlers only read + dispatch (none call <c>SaveChanges</c>),
/// so there is no re-entrant save.
/// </para>
/// </summary>
public sealed class DomainEventDispatcher : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;

    public DomainEventDispatcher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var ctx = eventData.Context;
        if (ctx is null) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var entriesWithEvents = ctx.ChangeTracker.Entries()
            .Select(e => e.Entity)
            .OfType<AggregateRoot<System.Guid>>()
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();

        var allEvents = entriesWithEvents.SelectMany(e => e.DomainEvents).ToList();

        foreach (var entity in entriesWithEvents)
        {
            entity.ClearDomainEvents();
        }

        foreach (var domainEvent in allEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
