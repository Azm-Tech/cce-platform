using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Evaluation.DTOs;
using CCE.Application.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CCE.Application.Common.Pagination;

namespace CCE.Application.Evaluation.Queries.GetAllEvaluations;

public sealed class GetAllEvaluationsQueryHandler
    : IRequestHandler<GetAllEvaluationsQuery, 
    Response<PagedResult<ServiceEvaluationDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetAllEvaluationsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PagedResult<ServiceEvaluationDto>>> Handle(
        GetAllEvaluationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.ServiceEvaluations
            .OrderByDescending(e => e.CreatedOn);
        var page = await query.ToPagedResultAsync(
            request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);
        var result = page.Map(e => new ServiceEvaluationDto(
            e.Id,
            e.OverallSatisfaction,
            e.EaseOfUse,
            e.ContentSuitability,
            e.Feedback,
            e.UserId,
            e.CreatedOn,
            e.CreatedById));
        return _msg.Ok(result, MessageKeys.General.ITEMS_LISTED);
    }
}
