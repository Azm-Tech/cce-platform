using CCE.Domain.Common;
using CCE.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

/// <summary>
/// Seeds one deterministic demo user per CCE role (cce-admin, cce-content-manager,
/// cce-reviewer, cce-expert, cce-user) with a known password.
///
/// Runs in <b>all environments</b> and is idempotent — skips users that
/// already exist by email address.
///
/// Order = 15 ensures roles are already present (RolesAndPermissionsSeeder = 10).
/// </summary>
public sealed class DemoUsersSeeder : ISeeder
{
    private readonly UserManager<User> _userManager;
    private readonly ISystemClock _clock;
    private readonly ILogger<DemoUsersSeeder> _logger;

    public DemoUsersSeeder(UserManager<User> userManager, ISystemClock clock, ILogger<DemoUsersSeeder> logger)
    {
        _userManager = userManager;
        _clock = clock;
        _logger = logger;
    }

    public int Order => 15;

    private static readonly (string Email, string Password, string Role, string FirstName, string LastName)[] Users =
    {
        ("superadmin@cce.local",       "SuperAdminPass123!",  "cce-super-admin",    "Super",   "Admin"),
        ("ahmed.elbatal@azm.com",      "SuperAdminPass123!",  "cce-super-admin",    "Ahmed",   "Elbatal"),
        ("admin@cce.local",            "AdminPass123!",       "cce-admin",          "System",  "Admin"),
        ("contentmgr@cce.local",       "ContentMgrPass123!",  "cce-content-manager", "Content", "Manager"),
        ("staterep@cce.local",         "StateRepPass123!",    "cce-state-representative",      "State",   "Representative"),
        ("reviewer@cce.local",         "ReviewerPass1!",      "cce-reviewer",       "Content", "Reviewer"),
        ("expert@cce.local",           "ExpertPass123!",      "cce-expert",         "Domain",  "Expert"),
        ("user@cce.local",             "UserPass12345!",      "cce-user",           "Regular", "User"),
    };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var (email, password, role, firstName, lastName) in Users)
        {
            var existing = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
            if (existing is not null)
            {
                _logger.LogInformation("Demo user {Email} already exists — skipping.", email);
                continue;
            }

            var user = User.RegisterLocal(firstName, lastName, email, "Demo", "CCE", "", _clock);
            user.EmailConfirmed = true;

            var createResult = await _userManager.CreateAsync(user, password).ConfigureAwait(false);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(static e => e.Description));
                _logger.LogError("Failed to create demo user {Email}: {Errors}", email, errors);
                continue;
            }

            var roleResult = await _userManager.AddToRoleAsync(user, role).ConfigureAwait(false);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(static e => e.Description));
                _logger.LogError("Failed to assign role {Role} to {Email}: {Errors}", role, email, errors);
            }
            else
            {
                _logger.LogInformation("Created demo user {Email} with role {Role}.", email, role);
            }
        }
    }
}
