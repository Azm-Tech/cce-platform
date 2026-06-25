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
    : IRequestHandler<GetSatisfactionSurveyReportQuery, Response<List<SatisfactionSurveyReportDto>>>
{
    public async Task<Response<List<SatisfactionSurveyReportDto>>> Handle(
        GetSatisfactionSurveyReportQuery q, CancellationToken ct)
    {
        var items = await _db.ServiceEvaluations
            .OrderByDescending(e => e.CreatedOn)
            .Select(e => new SatisfactionSurveyReportDto(
                e.Id,
                (int)e.OverallSatisfaction,
                (int)e.EaseOfUse,
                (int)e.ContentSuitability,
                e.Feedback,
                e.UserId,
                e.CreatedOn))
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);

        return _msg.Ok(items, MessageKeys.General.ITEMS_LISTED);
    }
}
