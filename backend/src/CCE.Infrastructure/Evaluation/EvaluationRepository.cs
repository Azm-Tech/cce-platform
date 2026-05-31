using CCE.Application.Evaluation;
using CCE.Domain.Evaluation;
using CCE.Infrastructure.Persistence;

namespace CCE.Infrastructure.Evaluation;

public sealed class EvaluationRepository : IEvaluationRepository
{
    private readonly CceDbContext _db;

    public EvaluationRepository(CceDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(ServiceEvaluation evaluation, CancellationToken ct)
    {
        _db.ServiceEvaluations.Add(evaluation);
    }
}
