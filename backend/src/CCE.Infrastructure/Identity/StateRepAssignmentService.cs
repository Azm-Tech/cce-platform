using CCE.Application.Identity;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Identity;

public sealed class StateRepAssignmentService : IStateRepAssignmentService
{
    private readonly CceDbContext _db;

    public StateRepAssignmentService(CceDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(StateRepresentativeAssignment assignment, CancellationToken ct)
    {
        _db.StateRepresentativeAssignments.Add(assignment);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<StateRepresentativeAssignment?> FindIncludingRevokedAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.StateRepresentativeAssignments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(StateRepresentativeAssignment assignment, CancellationToken ct)
    {
        // Entity is already tracked from FindIncludingRevokedAsync; SaveChanges flushes.
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
