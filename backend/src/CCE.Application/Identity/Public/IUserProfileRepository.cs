using CCE.Domain.Identity;

namespace CCE.Application.Identity.Public;

public interface IUserProfileRepository
{
    Task<User?> FindAsync(System.Guid userId, CancellationToken ct);
    Task UpdateAsync(User user, CancellationToken ct);
}
