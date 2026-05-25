using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Evaluation.DTOs;
using CCE.Application.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Evaluation.Queries.GetAllEvaluations;

public sealed class GetAllEvaluationsQueryHandler
    : IRequestHandler<GetAllEvaluationsQuery, Response<List<ServiceEvaluationDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetAllEvaluationsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<List<ServiceEvaluationDto>>> Handle(
        GetAllEvaluationsQuery request,
        CancellationToken cancellationToken)
    {
        var evaluations = await _db.ServiceEvaluations
            .OrderByDescending(e => e.CreatedOn)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var dtos = evaluations.Select(e => new ServiceEvaluationDto(
            e.Id,
            e.OverallSatisfaction,
            e.EaseOfUse,
            e.ContentSuitability,
            e.Feedback,
            e.UserId,
            e.CreatedOn,
            e.CreatedById)).ToList();

        return _msg.Ok(dtos, "ITEMS_LISTED");
    }
}
