using System.Security.Claims;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Api.Common.Auth;

/// <summary>
/// Sub-11 — lazy UPN → Entra ID objectId linker. Called once per sign-in
/// from BFF's OnTokenValidated event (web-portal, admin-cms BFFs).
///
/// On first sign-in post-cutover: matches an existing CCE User row by
/// Email or UserName == UPN, and sets EntraIdObjectId. If no match,
/// creates a stub User row (external partner-tenant user with no
/// pre-existing CCE row).
///
/// Subsequent sign-ins are no-ops.
///
/// Concurrency: filtered unique index ix_asp_net_users_entra_id_object_id
/// enforces no two rows share the same objectId. Concurrent first-sign-ins
/// of the same user → DB throws DbUpdateException on the loser; resolver
/// swallows + logs.
///
/// Resilience: SaveChangesAsync failures are logged but do NOT block
/// sign-in. Subsequent sign-in retries the link.
/// </summary>
public sealed class EntraIdUserResolver
{
    private readonly CceDbContext _db;
    private readonly ILogger<EntraIdUserResolver> _logger;

    public EntraIdUserResolver(CceDbContext db, ILogger<EntraIdUserResolver> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnsureLinkedAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        var oidClaim = principal.FindFirstValue("oid");
        if (string.IsNullOrWhiteSpace(oidClaim) || !Guid.TryParse(oidClaim, out var objectId))
        {
            // Token has no oid (malformed / wrong issuer); auth pipeline rejects elsewhere.
            return;
        }

        // Already linked? Common case after first sign-in.
        var alreadyLinked = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.EntraIdObjectId == objectId, ct).ConfigureAwait(false);
        if (alreadyLinked) return;

        var upn = principal.FindFirstValue("preferred_username")
               ?? principal.FindFirstValue(ClaimTypes.Upn)
               ?? principal.FindFirstValue(ClaimTypes.Email)
               ?? principal.FindFirstValue("email");
        if (string.IsNullOrWhiteSpace(upn))
        {
            _logger.LogWarning("Token has oid={Oid} but no UPN/email claim; cannot link to CCE user.", objectId);
            return;
        }

        try
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == upn || u.UserName == upn, ct).ConfigureAwait(false);
            if (user is null)
            {
                // External partner-tenant user with no pre-existing CCE row.
                var stub = User.CreateStubFromEntraId(objectId, upn, principal.Identity?.Name ?? upn);
                _db.Users.Add(stub);
                _logger.LogInformation("Created stub CCE User for new Entra ID user oid={Oid} upn={Upn}", objectId, upn);
            }
            else
            {
                user.LinkEntraIdObjectId(objectId);
                _logger.LogInformation("Linked existing CCE User {UserId} to Entra ID oid={Oid}", user.Id, objectId);
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            // Concurrent link attempt; the other call won. No-op.
            _logger.LogInformation(ex, "Concurrent EntraIdObjectId link race; subsequent sign-in will see linked state.");
        }
        catch (Exception ex)
        {
            // Don't block sign-in. Log + carry on; next sign-in retries.
            _logger.LogError(ex, "Failed to link CCE User to Entra ID oid={Oid}; sign-in will proceed unlinked.", objectId);
        }
    }
}
