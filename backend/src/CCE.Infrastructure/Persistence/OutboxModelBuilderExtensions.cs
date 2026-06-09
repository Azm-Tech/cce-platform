using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Persistence;

/// <summary>
/// Adds the MassTransit EF Core transactional-outbox entities to the model. Kept in its own file so the
/// blanket <c>using MassTransit;</c> needed for these extension methods doesn't collide with domain type
/// names that also exist in the MassTransit namespace (e.g. <c>Event</c>, <c>ConcurrencyException</c>).
/// </summary>
internal static class OutboxModelBuilderExtensions
{
    public static void AddMassTransitOutboxEntities(this ModelBuilder builder)
    {
        builder.AddInboxStateEntity();
        builder.AddOutboxStateEntity();
        builder.AddOutboxMessageEntity();
    }
}
