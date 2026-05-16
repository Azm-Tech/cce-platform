using CCE.Application.Identity;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Identity;

public sealed class StateRepAssignmentRepository : Repository<StateRepresentativeAssignment, System.Guid>, IStateRepAssignmentRepository
{
    public StateRepAssignmentRepository(CceDbContext db) : base(db) { }

    public async Task<StateRepresentativeAssignment?> FindIncludingRevokedAsync(System.Guid id, CancellationToken ct)
    {
        return await Db.StateRepresentativeAssignments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            .ConfigureAwait(false);
    }
}
