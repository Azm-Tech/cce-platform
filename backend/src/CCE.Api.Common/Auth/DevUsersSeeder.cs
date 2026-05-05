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
                db.Users.Add(new User
                {
                    Id = userId,
                    UserName = email,
                    NormalizedUserName = email.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    EmailConfirmed = true,
                });
                _logger.LogInformation("DevUsersSeeder: seeded {Role} user {UserId}", role, userId);
            }
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DevUsersSeeder failed; dev login may not work end-to-end");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
