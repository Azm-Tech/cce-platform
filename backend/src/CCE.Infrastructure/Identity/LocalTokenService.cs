using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CCE.Application.Identity.Auth.Common;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CCE.Infrastructure.Identity;

public sealed class LocalTokenService : ILocalTokenService
{
    private readonly UserManager<User> _userManager;
    private readonly ISystemClock _clock;
    private readonly IOptions<LocalAuthOptions> _options;

    public LocalTokenService(
        UserManager<User> userManager,
        ISystemClock clock,
        IOptions<LocalAuthOptions> options)
    {
        _userManager = userManager;
        _clock = clock;
        _options = options;
    }

    public async Task<TokenIssueResult> IssueAsync(User user, LocalAuthApi api, CancellationToken ct)
    {
        var opts = _options.Value;
        var profile = opts.GetProfile(api);
        ValidateProfile(profile);

        var now = _clock.UtcNow;
        var accessExpires = now.AddMinutes(opts.AccessTokenMinutes);
        var refreshExpires = now.AddDays(opts.RefreshTokenDays);
        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new("preferred_username", user.UserName ?? user.Email ?? string.Empty),
            new("email", user.Email ?? string.Empty),
        };
        claims.AddRange(roles.Select(role => new Claim("roles", role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(profile.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: profile.Issuer,
            audience: profile.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: accessExpires.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        return new TokenIssueResult(
            accessToken,
            accessExpires,
            refreshToken,
            HashRefreshToken(refreshToken),
            refreshExpires);
    }

    public string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }

    private static string GenerateRefreshToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
    }

    private static void ValidateProfile(LocalAuthJwtProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Issuer)
            || string.IsNullOrWhiteSpace(profile.Audience)
            || Encoding.UTF8.GetByteCount(profile.SigningKey) < 32)
        {
            throw new InvalidOperationException("LocalAuth issuer, audience, and a 32+ byte signing key are required.");
        }
    }
}
