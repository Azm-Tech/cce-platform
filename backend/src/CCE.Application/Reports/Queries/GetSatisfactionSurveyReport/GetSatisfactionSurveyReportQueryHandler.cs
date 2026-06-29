using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.Reports.Dtos;
using MediatR;

namespace CCE.Application.Reports.Queries.GetSatisfactionSurveyReport;

internal sealed class GetSatisfactionSurveyReportQueryHandler(
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<GetSatisfactionSurveyReportQuery, Response<PagedResult<SatisfactionSurveyReportDto>>>
{
    public async Task<Response<PagedResult<SatisfactionSurveyReportDto>>> Handle(
        GetSatisfactionSurveyReportQuery q, CancellationToken ct)
    {
        var query = _db.ServiceEvaluations.AsQueryable();

        if (q.From.HasValue)
            query = query.Where(e => e.CreatedOn >= q.From.Value);
        if (q.To.HasValue)
            query = query.Where(e => e.CreatedOn <= q.To.Value);

        query = query.OrderByDescending(e => e.CreatedOn);

        var paged = await query.ToPagedResultAsync(
            e => new SatisfactionSurveyReportDto(
                e.Id,
                e.OverallSatisfaction,
                e.EaseOfUse,
                e.ContentSuitability,
                e.Feedback,
                e.UserId,
                e.CreatedOn),
            q.Page,
            q.PageSize,
            ct)
            .ConfigureAwait(false);

        return _msg.Ok(paged, MessageKeys.General.ITEMS_LISTED);
    }
}
