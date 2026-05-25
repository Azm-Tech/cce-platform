using CCE.Application.Evaluation.DTOs;
using MediatR;

namespace CCE.Application.Evaluation.Queries.GetEvaluationById;

public sealed class GetEvaluationByIdQueryHandler
    : IRequestHandler<GetEvaluationByIdQuery, ServiceEvaluationDto?>
{
    private readonly IEvaluationRepository _repository;

    public GetEvaluationByIdQueryHandler(IEvaluationRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceEvaluationDto?> Handle(
        GetEvaluationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var evaluation = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (evaluation is null) return null;

        return new ServiceEvaluationDto(
            evaluation.Id,
            evaluation.OverallSatisfaction,
            evaluation.EaseOfUse,
            evaluation.ContentSuitability,
            evaluation.Feedback,
            evaluation.UserId,
            evaluation.CreatedOn,
            evaluation.CreatedById);
    }
}
