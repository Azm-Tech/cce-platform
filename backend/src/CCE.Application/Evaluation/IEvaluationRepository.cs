using CCE.Domain.Evaluation;

namespace CCE.Application.Evaluation;

public interface IEvaluationRepository
{
    Task AddAsync(ServiceEvaluation evaluation, CancellationToken ct);
    Task<List<ServiceEvaluation>> GetAllAsync(CancellationToken ct);
    Task<ServiceEvaluation?> GetByIdAsync(System.Guid id, CancellationToken ct);
}
