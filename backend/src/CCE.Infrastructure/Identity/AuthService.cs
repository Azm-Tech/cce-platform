using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Auth.Common;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Identity;

public sealed class AuthService : IAuthService
{
    private const string DefaultRole = "cce-user";
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ILocalTokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ICceDbContext _db;
    private readonly ISystemClock _clock;
    private readonly IOptions<LocalAuthOptions> _options;
    private readonly IPasswordResetEmailSender _emailSender;

    public AuthService(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ILocalTokenService tokenService,
        IRefreshTokenRepository refreshTokens,
        ICceDbContext db,
        ISystemClock clock,
        IOptions<LocalAuthOptions> options,
        IPasswordResetEmailSender emailSender)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _db = db;
        _clock = clock;
        _options = options;
        _emailSender = emailSender;
    }

    public async Task<AuthTokenDto?> LoginAsync(string email, string password, LocalAuthApi api, string? ip, string? userAgent, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (user is null) return null;

        if (_options.Value.RequireConfirmedEmail && !await _userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false))
            return null;

        if (!await _userManager.CheckPasswordAsync(user, password).ConfigureAwait(false))
            return null;

        return await IssueAndBuildDtoAsync(user, api, ip, userAgent, null, ct).ConfigureAwait(false);
    }

    public async Task<AuthTokenDto?> RefreshTokenAsync(string rawRefreshToken, LocalAuthApi api, string? ip, string? userAgent, CancellationToken ct)
    {
        var tokenHash = _tokenService.HashRefreshToken(rawRefreshToken);
        var existing = await _refreshTokens.FindByHashAsync(tokenHash, ct).ConfigureAwait(false);
        if (existing is null) return null;

        if (!existing.IsActive(_clock.UtcNow))
        {
            if (existing.RevokedAtUtc is not null)
            {
                await _refreshTokens.RevokeFamilyAsync(existing.TokenFamilyId, _clock.UtcNow, ip, ct).ConfigureAwait(false);
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            return null;
        }

        var user = await _userManager.FindByIdAsync(existing.UserId.ToString()).ConfigureAwait(false);
        if (user is null) return null;

        var issued = await _tokenService.IssueAsync(user, api, ct).ConfigureAwait(false);
        existing.Revoke(_clock.UtcNow, ip, issued.RefreshTokenHash);

        var replacement = global::CCE.Domain.Identity.RefreshToken.Create(
            user.Id, issued.RefreshTokenHash, existing.TokenFamilyId,
            _clock.UtcNow, issued.RefreshTokenExpiresAtUtc, ip, userAgent);
        await _refreshTokens.AddAsync(replacement, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return await BuildDtoAsync(user, issued).ConfigureAwait(false);
    }

    public async Task LogoutAsync(string rawRefreshToken, string? ip, CancellationToken ct)
    {
        var tokenHash = _tokenService.HashRefreshToken(rawRefreshToken);
        var existing = await _refreshTokens.FindByHashAsync(tokenHash, ct).ConfigureAwait(false);
        if (existing is not null && existing.IsActive(_clock.UtcNow))
        {
            existing.Revoke(_clock.UtcNow, ip);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<RegisterResult> RegisterAsync(string firstName, string lastName, string email, string password, string? jobTitle, string? orgName, string? phone, CancellationToken ct)
    {
        var existing = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (existing is not null) return new RegisterResult(null, true);

        var user = User.RegisterLocal(firstName, lastName, email, jobTitle ?? "", orgName ?? "", phone ?? "");

        var createResult = await _userManager.CreateAsync(user, password).ConfigureAwait(false);
        if (!createResult.Succeeded) return new RegisterResult(null, false);

        if (!await _roleManager.RoleExistsAsync(DefaultRole).ConfigureAwait(false))
        {
            var roleResult = await _roleManager.CreateAsync(new Role(DefaultRole)).ConfigureAwait(false);
            if (!roleResult.Succeeded) return new RegisterResult(null, false);
        }

        var addRoleResult = await _userManager.AddToRoleAsync(user, DefaultRole).ConfigureAwait(false);
        if (!addRoleResult.Succeeded) return new RegisterResult(null, false);

        return new RegisterResult(user, false);
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
            await _emailSender.SendAsync(user, PasswordResetTokenCodec.Encode(token), ct).ConfigureAwait(false);
        }
    }

    public async Task<string?> ResetPasswordAsync(string email, string encodedToken, string newPassword, string? ip, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (user is null) return "USER_NOT_FOUND";

        string token;
        try
        {
            token = PasswordResetTokenCodec.Decode(encodedToken);
        }
        catch (FormatException)
        {
            return "INVALID_RESET_TOKEN";
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword).ConfigureAwait(false);
        if (!result.Succeeded) return "RESET_FAILED";

        await _userManager.UpdateSecurityStampAsync(user).ConfigureAwait(false);
        await _refreshTokens.RevokeAllForUserAsync(user.Id, _clock.UtcNow, ip, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return null;
    }

    private async Task<AuthTokenDto> IssueAndBuildDtoAsync(User user, LocalAuthApi api, string? ip, string? userAgent, Guid? tokenFamilyId, CancellationToken ct)
    {
        var issued = await _tokenService.IssueAsync(user, api, ct).ConfigureAwait(false);
        var familyId = tokenFamilyId ?? Guid.NewGuid();
        var refreshToken = global::CCE.Domain.Identity.RefreshToken.Create(
            user.Id, issued.RefreshTokenHash, familyId,
            _clock.UtcNow, issued.RefreshTokenExpiresAtUtc, ip, userAgent);
        await _refreshTokens.AddAsync(refreshToken, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return await BuildDtoAsync(user, issued).ConfigureAwait(false);
    }

    private async Task<AuthTokenDto> BuildDtoAsync(User user, TokenIssueResult issued)
    {
        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        return new AuthTokenDto(
            issued.AccessToken,
            issued.AccessTokenExpiresAtUtc,
            issued.RefreshToken,
            issued.RefreshTokenExpiresAtUtc,
            "Bearer",
            new AuthUserDto(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, roles.ToArray()));
    }
}
