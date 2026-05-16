using CCE.Application.Identity.Auth.Common;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Identity;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly CceDbContext _db;

    public RefreshTokenRepository(CceDbContext db) => _db = db;

    public async Task AddAsync(RefreshToken token, CancellationToken ct)
        => await _db.RefreshTokens.AddAsync(token, ct).ConfigureAwait(false);

    public async Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct)
        => await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct)
            .ConfigureAwait(false);

    public async Task RevokeFamilyAsync(Guid tokenFamilyId, DateTimeOffset revokedAtUtc, string? revokedByIp, CancellationToken ct)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.TokenFamilyId == tokenFamilyId && t.RevokedAtUtc == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var token in tokens)
        {
            token.Revoke(revokedAtUtc, revokedByIp);
        }
    }

    public async Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAtUtc, string? revokedByIp, CancellationToken ct)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var token in tokens)
        {
            token.Revoke(revokedAtUtc, revokedByIp);
        }
    }

}
