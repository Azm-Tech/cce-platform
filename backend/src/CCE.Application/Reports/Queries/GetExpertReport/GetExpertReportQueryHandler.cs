using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.Reports.Dtos;
using MediatR;

namespace CCE.Application.Reports.Queries.GetExpertReport;

internal sealed class GetExpertReportQueryHandler(
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<GetExpertReportQuery, Response<PagedResult<ExpertReportDto>>>
{
    public async Task<Response<PagedResult<ExpertReportDto>>> Handle(
        GetExpertReportQuery q, CancellationToken ct)
    {
        var query = from ep in _db.ExpertProfiles
                    join u in _db.Users on ep.UserId equals u.Id
                    select new
                    {
                        ep.Id,
                        UserId = u.Id,
                        u.FirstName,
                        u.LastName,
                        u.Email,
                        u.JobTitle,
                        u.OrganizationName,
                        ep.BioAr,
                        ep.BioEn,
                        ep.AcademicTitleAr,
                        ep.AcademicTitleEn,
                        ep.ExpertiseTags,
                        ep.ApprovedOn
                    };

        if (q.From.HasValue)
            query = query.Where(x => x.ApprovedOn >= q.From.Value);
        if (q.To.HasValue)
            query = query.Where(x => x.ApprovedOn <= q.To.Value);

        query = query.OrderByDescending(x => x.ApprovedOn);

        var paged = await query.ToPagedResultAsync(
            x => new ExpertReportDto(
                x.Id,
                x.UserId,
                x.FirstName,
                x.LastName,
                x.Email,
                x.JobTitle,
                x.OrganizationName,
                x.BioAr,
                x.BioEn,
                x.AcademicTitleAr,
                x.AcademicTitleEn,
                x.ExpertiseTags.ToList(),
                x.ApprovedOn),
            q.Page,
            q.PageSize,
            ct)
            .ConfigureAwait(false);

        return _msg.Ok(paged, MessageKeys.General.ITEMS_LISTED);
    }
}
