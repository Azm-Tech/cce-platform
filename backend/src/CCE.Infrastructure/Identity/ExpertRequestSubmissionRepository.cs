using CCE.Application.Identity.Public;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;

namespace CCE.Infrastructure.Identity;

public sealed class ExpertRequestSubmissionRepository : IExpertRequestSubmissionRepository
{
    private readonly CceDbContext _db;

    public ExpertRequestSubmissionRepository(CceDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(ExpertRegistrationRequest request, CancellationToken ct)
    {
        _db.ExpertRegistrationRequests.Add(request);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
