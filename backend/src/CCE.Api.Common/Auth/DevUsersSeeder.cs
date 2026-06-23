using CCE.Domain.Common;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CCE.Api.Common.Auth;

/// <summary>
/// Sub-11d follow-up — seeds CCE.DB with one User row per dev role
/// (cce-admin / editor / reviewer / expert / user) so <c>/api/me</c>
/// resolves cleanly when <see cref="DevAuthHandler"/> synthesizes a
/// principal with the matching deterministic <c>sub</c> claim.
///
/// Runs once at host startup via <see cref="IHostedService"/>; idempotent
/// (skips rows that already exist by Id).
/// </summary>
public sealed class DevUsersSeeder : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DevUsersSeeder> _logger;

    public DevUsersSeeder(IServiceProvider services, ILogger<DevUsersSeeder> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CceDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<ISystemClock>();

        try
        {
            foreach (var (role, userId) in DevAuthHandler.RoleToUserId)
            {
                var existing = await db.Users
                    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                    .ConfigureAwait(false);
                if (existing is not null)
                {
                    continue;
                }

                var email = $"{role}@cce.local";
                var u = new User
                {
                    Id = userId,
                    UserName = email,
                    NormalizedUserName = email.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    EmailConfirmed = true,
                };
                u.MarkAsCreated(userId, clock);
                db.Users.Add(u);
                _logger.LogInformation("DevUsersSeeder: seeded {Role} user {UserId}", role, userId);
            }
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Seed ExpertProfile for the cce-expert dev user so the SQL read-merge path in
            // ListUserFeedQueryHandler is exercised in dev mode (ExpertProfiles JOIN).
            var expertUserId = DevAuthHandler.RoleToUserId["cce-expert"];
            var expertExists = await db.ExpertProfiles
                .AnyAsync(e => e.UserId == expertUserId, cancellationToken)
                .ConfigureAwait(false);
            if (!expertExists)
            {
                var now = clock.UtcNow;
                var adminId = DevAuthHandler.RoleToUserId["cce-admin"];
                await db.Database.ExecuteSqlRawAsync(
                    """
                    INSERT INTO expert_profiles
                        (id, user_id, bio_ar, bio_en, expertise_tags,
                         academic_title_ar, academic_title_en,
                         approved_on, approved_by_id,
                         created_on, created_by_id, is_deleted)
                    VALUES
                        ({0}, {1}, N'', N'', N'[]',
                         N'Dev Expert', N'Dev Expert',
                         {2}, {3},
                         {2}, {3}, 0)
                    """,
                    Guid.NewGuid(), expertUserId, now, adminId)
                    .ConfigureAwait(false);
                _logger.LogInformation("DevUsersSeeder: seeded ExpertProfile for cce-expert user {UserId}", expertUserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DevUsersSeeder failed; dev login may not work end-to-end");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
