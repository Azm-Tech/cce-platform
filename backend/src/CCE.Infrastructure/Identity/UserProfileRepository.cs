using CCE.Application.Identity.Public;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Identity;

public sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly CceDbContext _db;

    public UserProfileRepository(CceDbContext db)
    {
        _db = db;
    }

    public async Task<User?> FindAsync(System.Guid userId, CancellationToken ct)
        => await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct).ConfigureAwait(false);

    public async Task UpdateAsync(User user, CancellationToken ct)
        => await _db.SaveChangesAsync(ct).ConfigureAwait(false);
}
