using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.Reports.Dtos;
using MediatR;

namespace CCE.Application.Reports.Queries.GetUserPreferenceReport;

internal sealed class GetUserPreferenceReportQueryHandler(
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<GetUserPreferenceReportQuery, Response<PagedResult<UserPreferenceReportDto>>>
{
    public async Task<Response<PagedResult<UserPreferenceReportDto>>> Handle(
        GetUserPreferenceReportQuery q, CancellationToken ct)
    {
        var query = from u in _db.Users.Where(u => !u.IsDeleted)
                    join c in _db.Countries on u.CountryId equals c.Id into cJoin
                    from c in cJoin.DefaultIfEmpty()
                    select new { u, c };

        if (q.From.HasValue)
            query = query.Where(x => x.u.CreatedOn >= q.From.Value);
        if (q.To.HasValue)
            query = query.Where(x => x.u.CreatedOn <= q.To.Value);

        query = query.OrderByDescending(x => x.u.CreatedOn);

        var paged = await query.ToPagedResultAsync(
            x => new UserPreferenceReportDto(
                x.u.Id,
                x.u.UserInterestTopics.Select(uit => new AreaOfInterestDto(
                    uit.InterestTopicId,
                    uit.InterestTopic.NameAr,
                    uit.InterestTopic.NameEn
                )).ToList(),
                x.u.KnowledgeLevel,
                x.u.JobTitle,
                x.u.CountryId,
                x.c != null ? x.c.NameAr : null,
                x.c != null ? x.c.NameEn : null),
            q.Page,
            q.PageSize,
            ct)
            .ConfigureAwait(false);

        return _msg.Ok(paged, MessageKeys.General.ITEMS_LISTED);
    }
}
