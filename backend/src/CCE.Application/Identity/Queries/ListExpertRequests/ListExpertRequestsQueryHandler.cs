using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Identity.Queries.ListExpertRequests;

public sealed class ListExpertRequestsQueryHandler
    : IRequestHandler<ListExpertRequestsQuery, PagedResult<ExpertRequestDto>>
{
    private readonly ICceDbContext _db;

    public ListExpertRequestsQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PagedResult<ExpertRequestDto>> Handle(
        ListExpertRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.ExpertRegistrationRequests.AsQueryable();
        if (request.Status is not null)
        {
            query = query.Where(r => r.Status == request.Status.Value);
        }
        if (request.RequestedById is not null)
        {
            query = query.Where(r => r.RequestedById == request.RequestedById.Value);
        }
        query = query.OrderByDescending(r => r.SubmittedOn);

        var paged = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);

        if (paged.Items.Count == 0)
        {
            return new PagedResult<ExpertRequestDto>(
                Array.Empty<ExpertRequestDto>(), paged.Page, paged.PageSize, paged.Total);
        }

        var requesterIds = paged.Items.Select(r => r.RequestedById).Distinct().ToList();
        var userNamesQuery =
            from u in _db.Users
            where requesterIds.Contains(u.Id)
            select new UserNameRow(u.Id, u.UserName);
        var userNameRows = await userNamesQuery.ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var nameByUserId = userNameRows.ToDictionary(r => r.UserId, r => r.UserName);

        var items = paged.Items.Select(r => new ExpertRequestDto(
            r.Id,
            r.RequestedById,
            nameByUserId.TryGetValue(r.RequestedById, out var name) ? name : null,
            r.RequestedBioAr,
            r.RequestedBioEn,
            r.RequestedTags.ToList(),
            r.SubmittedOn,
            r.Status,
            r.ProcessedById,
            r.ProcessedOn,
            r.RejectionReasonAr,
            r.RejectionReasonEn)).ToList();

        return new PagedResult<ExpertRequestDto>(items, paged.Page, paged.PageSize, paged.Total);
    }

    private sealed record UserNameRow(Guid UserId, string? UserName);
}
