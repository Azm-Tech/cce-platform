using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Identity.Queries.ListExpertProfiles;

public sealed class ListExpertProfilesQueryHandler
    : IRequestHandler<ListExpertProfilesQuery, PagedResult<ExpertProfileDto>>
{
    private readonly ICceDbContext _db;

    public ListExpertProfilesQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ExpertProfileDto>> Handle(
        ListExpertProfilesQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<ExpertProfile> query = _db.ExpertProfiles;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = from p in query
                    join u in _db.Users on p.UserId equals u.Id
                    where (u.UserName != null && u.UserName.Contains(term))
                       || (u.Email != null && u.Email.Contains(term))
                    select p;
        }

        query = query.OrderByDescending(p => p.ApprovedOn);

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        if (page.Items.Count == 0)
        {
            return new PagedResult<ExpertProfileDto>(
                System.Array.Empty<ExpertProfileDto>(),
                page.Page, page.PageSize, page.Total);
        }

        var userIds = page.Items.Select(p => p.UserId).Distinct().ToList();
        var userNamesQuery =
            from u in _db.Users
            where userIds.Contains(u.Id)
            select new UserNameRow(u.Id, u.UserName);
        var userNameRows = await userNamesQuery.ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var nameByUserId = userNameRows.ToDictionary(r => r.UserId, r => r.UserName);

        var items = page.Items.Select(p => new ExpertProfileDto(
            p.Id,
            p.UserId,
            nameByUserId.TryGetValue(p.UserId, out var name) ? name : null,
            p.BioAr,
            p.BioEn,
            p.ExpertiseTags.ToList(),
            p.AcademicTitleAr,
            p.AcademicTitleEn,
            p.ApprovedOn,
            p.ApprovedById)).ToList();

        return new PagedResult<ExpertProfileDto>(items, page.Page, page.PageSize, page.Total);
    }

    private sealed record UserNameRow(System.Guid UserId, string? UserName);
}
