using CCE.Domain.Identity;

namespace CCE.Application.Identity.Auth.Common;

public interface IRefreshTokenRepository
{
    Task AddAsync(CCE.Domain.Identity.RefreshToken token, CancellationToken ct);

    Task<CCE.Domain.Identity.RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct);

    Task RevokeFamilyAsync(System.Guid tokenFamilyId, DateTimeOffset revokedAtUtc, string? revokedByIp, CancellationToken ct);

    Task RevokeAllForUserAsync(System.Guid userId, DateTimeOffset revokedAtUtc, string? revokedByIp, CancellationToken ct);
}
