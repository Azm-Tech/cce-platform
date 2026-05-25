using CCE.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CCE.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Spec §3.5 + §5.4. Post-commit interceptor that drains <see cref="IDomainEvent"/>s from
/// every aggregate root tracked by the context and publishes them via <see cref="IPublisher"/>
/// (MediatR). In-process synchronous handlers only (sub-project 2 requirement). Outbox is
/// sub-project 8 work.
/// </summary>
public sealed class DomainEventDispatcher : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;

    public DomainEventDispatcher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        var ctx = eventData.Context;
        if (ctx is null) return await base.SavedChangesAsync(eventData, result, cancellationToken);

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

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
