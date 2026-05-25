using CCE.Domain.Evaluation;

namespace CCE.Application.Evaluation;

public interface IEvaluationRepository
{
    Task AddAsync(ServiceEvaluation evaluation, CancellationToken ct);
}
