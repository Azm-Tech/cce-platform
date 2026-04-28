using CCE.Application.Identity;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Identity;

public sealed class UserSyncService : IUserSyncService
{
    private readonly CceDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserSyncService> _logger;

    public UserSyncService(CceDbContext db, IConfiguration configuration, ILogger<UserSyncService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EnsureUserExistsAsync(
        Guid userId,
        string email,
        string preferredUsername,
        IReadOnlyCollection<string> groupClaims,
        CancellationToken ct)
    {
        var existing = await _db.Users.FindAsync(new object[] { userId }, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        var user = new User
        {
            Id = userId,
            UserName = preferredUsername,
            NormalizedUserName = preferredUsername.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
        };
        _db.Users.Add(user);

        var groupToRole = _configuration.GetSection("UserSync:GroupToRoleMap")
            .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();

        foreach (var group in groupClaims)
        {
            if (!groupToRole.TryGetValue(group, out var roleName))
            {
                continue;
            }

            var role = await _db.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName, ct)
                .ConfigureAwait(false);
            if (role is null)
            {
                continue;
            }

            _db.UserRoles.Add(new IdentityUserRole<Guid>
            {
                UserId = userId,
                RoleId = role.Id,
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Synced new user {UserId} from JWT claims.", userId);
    }
}
