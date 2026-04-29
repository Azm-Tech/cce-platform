using CCE.Application.Audit.Dtos;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Domain.Audit;
using MediatR;

namespace CCE.Application.Audit.Queries.ListAuditEvents;

public sealed class ListAuditEventsQueryHandler
    : IRequestHandler<ListAuditEventsQuery, PagedResult<AuditEventDto>>
{
    private readonly ICceDbContext _db;

    public ListAuditEventsQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditEventDto>> Handle(
        ListAuditEventsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.AuditEvents.AsQueryable();

        if (request.Actor is { } actor)
            query = query.Where(e => e.Actor == actor);

        if (request.ActionPrefix is { } actionPrefix)
            query = query.Where(e => e.Action.StartsWith(actionPrefix + "."));

        if (request.ResourceType is { } resourceType)
            query = query.Where(e => e.Resource.StartsWith(resourceType + "/"));

        if (request.CorrelationId is { } correlationId)
            query = query.Where(e => e.CorrelationId == correlationId);

        if (request.From is { } from)
            query = query.Where(e => e.OccurredOn >= from);

        if (request.To is { } to)
            query = query.Where(e => e.OccurredOn <= to);

        query = query.OrderByDescending(e => e.OccurredOn);

        var page = await query
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items.Select(MapToDto).ToList();

        return new PagedResult<AuditEventDto>(items, page.Page, page.PageSize, page.Total);
    }

    private static AuditEventDto MapToDto(AuditEvent e) =>
        new(e.Id, e.OccurredOn, e.Actor, e.Action, e.Resource, e.CorrelationId, e.Diff);
}
