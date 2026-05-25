using CCE.Application.Evaluation.DTOs;
using MediatR;

namespace CCE.Application.Evaluation.Queries.GetAllEvaluations;

public sealed class GetAllEvaluationsQueryHandler
    : IRequestHandler<GetAllEvaluationsQuery, List<ServiceEvaluationDto>>
{
    private readonly IEvaluationRepository _repository;

    public GetAllEvaluationsQueryHandler(IEvaluationRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ServiceEvaluationDto>> Handle(
        GetAllEvaluationsQuery request,
        CancellationToken cancellationToken)
    {
        var evaluations = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        return evaluations.Select(e => new ServiceEvaluationDto(
            e.Id,
            e.OverallSatisfaction,
            e.EaseOfUse,
            e.ContentSuitability,
            e.Feedback,
            e.UserId,
            e.CreatedOn,
            e.CreatedById)).ToList();
    }
}
