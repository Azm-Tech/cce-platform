using CCE.Domain.Common;

namespace CCE.Domain.Identity;

public sealed class RefreshToken : Entity<System.Guid>
{
    private RefreshToken() : base(System.Guid.Empty) { }

    private RefreshToken(
        System.Guid id,
        System.Guid userId,
        string tokenHash,
        System.Guid tokenFamilyId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        string? createdByIp,
        string? userAgent)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        TokenFamilyId = tokenFamilyId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = createdByIp;
        UserAgent = userAgent;
    }

    public System.Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public System.Guid TokenFamilyId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? UserAgent { get; private set; }

    public bool IsActive(DateTimeOffset now) => RevokedAtUtc is null && ExpiresAtUtc > now;

    public static RefreshToken Create(
        System.Guid userId,
        string tokenHash,
        System.Guid tokenFamilyId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        string? createdByIp,
        string? userAgent)
    {
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new DomainException("TokenHash is required.");
        if (tokenFamilyId == System.Guid.Empty) throw new DomainException("TokenFamilyId is required.");
        if (expiresAtUtc <= createdAtUtc) throw new DomainException("Refresh token expiry must be after creation.");

        return new RefreshToken(
            System.Guid.NewGuid(),
            userId,
            tokenHash,
            tokenFamilyId,
            createdAtUtc,
            expiresAtUtc,
            createdByIp,
            userAgent);
    }

    public void Revoke(DateTimeOffset revokedAtUtc, string? revokedByIp, string? replacedByTokenHash = null)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = revokedAtUtc;
        RevokedByIp = revokedByIp;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
