using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Evaluation.DTOs;
using CCE.Application.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Evaluation.Queries.GetEvaluationById;

public sealed class GetEvaluationByIdQueryHandler
    : IRequestHandler<GetEvaluationByIdQuery, Response<ServiceEvaluationDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetEvaluationByIdQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<ServiceEvaluationDto>> Handle(
        GetEvaluationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var evaluation = await _db.ServiceEvaluations
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (evaluation is null)
            return _msg.EvaluationNotFound<ServiceEvaluationDto>();

        var dto = new ServiceEvaluationDto(
            evaluation.Id,
            evaluation.OverallSatisfaction,
            evaluation.EaseOfUse,
            evaluation.ContentSuitability,
            evaluation.Feedback,
            evaluation.UserId,
            evaluation.CreatedOn,
            evaluation.CreatedById);

        return _msg.Ok(dto, MessageKeys.General.ITEMS_LISTED);
    }
}
