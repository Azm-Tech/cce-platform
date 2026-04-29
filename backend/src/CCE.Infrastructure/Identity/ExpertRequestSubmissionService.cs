using CCE.Application.Identity.Public;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;

namespace CCE.Infrastructure.Identity;

public sealed class ExpertRequestSubmissionService : IExpertRequestSubmissionService
{
    private readonly CceDbContext _db;

    public ExpertRequestSubmissionService(CceDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(ExpertRegistrationRequest request, CancellationToken ct)
    {
        _db.ExpertRegistrationRequests.Add(request);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
