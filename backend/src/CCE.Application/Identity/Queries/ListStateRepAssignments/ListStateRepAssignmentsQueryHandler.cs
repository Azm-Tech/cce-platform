using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Identity.Queries.ListStateRepAssignments;

public sealed class ListStateRepAssignmentsQueryHandler
    : IRequestHandler<ListStateRepAssignmentsQuery, PagedResult<StateRepAssignmentDto>>
{
    private readonly ICceDbContext _db;

    public ListStateRepAssignmentsQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PagedResult<StateRepAssignmentDto>> Handle(
        ListStateRepAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<StateRepresentativeAssignment> query = request.Active
            ? _db.StateRepresentativeAssignments
            : _db.StateRepresentativeAssignments.WithoutSoftDeleteFilter();

        if (request.UserId is not null)
        {
            query = query.Where(a => a.UserId == request.UserId.Value);
        }
        if (request.CountryId is not null)
        {
            query = query.Where(a => a.CountryId == request.CountryId.Value);
        }

        query = query.OrderByDescending(a => a.AssignedOn);

        var paged = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);

        if (paged.Items.Count == 0)
        {
            return new PagedResult<StateRepAssignmentDto>(
                Array.Empty<StateRepAssignmentDto>(), paged.Page, paged.PageSize, paged.Total);
        }

        var userIds = paged.Items.Select(a => a.UserId).Distinct().ToList();
        var userNamesQuery =
            from u in _db.Users
            where userIds.Contains(u.Id)
            select new UserNameRow(u.Id, u.UserName);
        var userNameRows = await userNamesQuery.ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var nameByUserId = userNameRows.ToDictionary(r => r.UserId, r => r.UserName);

        var items = paged.Items.Select(a => new StateRepAssignmentDto(
            a.Id,
            a.UserId,
            nameByUserId.TryGetValue(a.UserId, out var name) ? name : null,
            a.CountryId,
            a.AssignedOn,
            a.AssignedById,
            a.RevokedOn,
            a.RevokedById,
            IsActive: a.RevokedOn is null && !a.IsDeleted)).ToList();

        return new PagedResult<StateRepAssignmentDto>(items, paged.Page, paged.PageSize, paged.Total);
    }

    private sealed record UserNameRow(Guid UserId, string? UserName);
}
