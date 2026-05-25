using CCE.Application.Evaluation;
using CCE.Domain.Evaluation;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<List<ServiceEvaluation>> GetAllAsync(CancellationToken ct)
    {
        return await _db.ServiceEvaluations
            .OrderByDescending(e => e.CreatedOn)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<ServiceEvaluation?> GetByIdAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.ServiceEvaluations
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            .ConfigureAwait(false);
    }
}
