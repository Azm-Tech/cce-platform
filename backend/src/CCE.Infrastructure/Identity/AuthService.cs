using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Auth.Common;
using CCE.Application.Notifications;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using CCE.Domain.Notifications;
using CCE.Integration.AdminAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
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
    private readonly INotificationGateway _gateway;
    private readonly IConfiguration _config;
    private readonly IAdminAuthGatewayClient _adGateway;

    public AuthService(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ILocalTokenService tokenService,
        IRefreshTokenRepository refreshTokens,
        ICceDbContext db,
        ISystemClock clock,
        IOptions<LocalAuthOptions> options,
        INotificationGateway gateway,
        IConfiguration config,
        IAdminAuthGatewayClient adGateway)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _db = db;
        _clock = clock;
        _options = options;
        _gateway = gateway;
        _config = config;
        _adGateway = adGateway;
    }

    public async Task<LoginResult> LoginAsync(string email, string password, LocalAuthApi api, string? ip, string? userAgent, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (user is null) return LoginResult.InvalidCredentials;

        if (_options.Value.RequireConfirmedEmail && !await _userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false))
            return LoginResult.InvalidCredentials;

        if (!await _userManager.CheckPasswordAsync(user, password).ConfigureAwait(false))
            return LoginResult.InvalidCredentials;

        // Credentials correct — but does the user have at least one verified contact?
        if (!await _userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false)
            && !await _userManager.IsPhoneNumberConfirmedAsync(user).ConfigureAwait(false))
            return LoginResult.ContactNotVerified;

        // Credentials are valid — but a deactivated account may not sign in.
        if (user.Status != UserStatus.Active)
            return LoginResult.Deactivated;

        if (api == LocalAuthApi.Internal)
        {
            var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
            if (roles.Count == 0 || roles.All(r => r == "cce-user" || r == "Anonymous"))
                return LoginResult.InvalidCredentials;
        }

        var token = await IssueAndBuildDtoAsync(user, api, ip, userAgent, null, ct).ConfigureAwait(false);
        return LoginResult.Success(token);
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

        // A deactivated account cannot refresh — revoke the whole family so existing
        // tokens stop working the moment the admin deactivates the user.
        if (user.Status != UserStatus.Active)
        {
            await _refreshTokens.RevokeFamilyAsync(existing.TokenFamilyId, _clock.UtcNow, ip, ct).ConfigureAwait(false);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return null;
        }

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

    public async Task<RegisterResult> RegisterAsync(string firstName, string lastName, string email, string password, string? jobTitle, string? orgName, string? phone, System.Guid? countryId, CancellationToken ct)
    {
        var existing = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (existing is not null) return new RegisterResult(null, true);

        var user = User.RegisterLocal(firstName, lastName, email, jobTitle ?? "", orgName ?? "", phone ?? "");
        if (countryId.HasValue) user.AssignCountry(countryId.Value);

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

    public async Task<AdminCreateResult> AdminCreateUserAsync(
        string firstName, string lastName, string email,
        string phone, System.Guid? countryId, string role, CancellationToken ct)
    {
        var existing = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (existing is not null) return new AdminCreateResult(null, true, false, false);

        var user = User.CreateByAdmin(firstName, lastName, email, phone);
        if (countryId.HasValue) user.AssignCountry(countryId.Value);

        var createResult = await _userManager.CreateAsync(user).ConfigureAwait(false);
        if (!createResult.Succeeded) return new AdminCreateResult(null, false, true, false);

        if (!await _roleManager.RoleExistsAsync(role).ConfigureAwait(false))
        {
            var roleResult = await _roleManager.CreateAsync(new Role(role)).ConfigureAwait(false);
            if (!roleResult.Succeeded) return new AdminCreateResult(null, false, true, false);
        }

        var addResult = await _userManager.AddToRoleAsync(user, role).ConfigureAwait(false);
        if (!addResult.Succeeded) return new AdminCreateResult(null, false, true, false);

        // Generate and send password-reset link so the user can set their own password.
        var token = await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
        var encodedToken = PasswordResetTokenCodec.Encode(token);
        var baseUrl = _config.GetValue<string>("Frontend:PasswordResetUrl")
            ?? "http://localhost:4100";
        var resetUrl = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(user.Email ?? string.Empty)}&token={Uri.EscapeDataString(encodedToken)}";

        await _gateway.SendAsync(new NotificationDispatchRequest(
            TemplateCode: "PASSWORD_RESET",
            RecipientUserId: user.Id,
            Channels: [NotificationChannel.Email],
            Variables: new Dictionary<string, string>
            {
                ["Name"] = user.FirstName,
                ["ResetUrl"] = resetUrl
            },
            Locale: user.LocalePreference,
            BypassSettings: true), ct).ConfigureAwait(false);

        return new AdminCreateResult(user, false, false, true);
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
            var encodedToken = PasswordResetTokenCodec.Encode(token);
            var baseUrl = _config.GetValue<string>("Frontend:PasswordResetUrl")
                ?? "http://localhost:4100";
            var resetUrl = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(user.Email ?? string.Empty)}&token={Uri.EscapeDataString(encodedToken)}";

            await _gateway.SendAsync(new NotificationDispatchRequest(
                TemplateCode: "PASSWORD_RESET",
                RecipientUserId: user.Id,
                Channels: [NotificationChannel.Email],
                Variables: new Dictionary<string, string>
                {
                    ["Name"] = user.FirstName,
                    ["ResetUrl"] = resetUrl
                },
                Locale: user.LocalePreference,
                BypassSettings: true), ct).ConfigureAwait(false);
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

    public async Task<LoginResult> AdLoginAsync(string username, string password, string? ip, string? userAgent, CancellationToken ct)
    {
        var gatewayResponse = await _adGateway.LoginAsync(
            new AdAuthRequest(username, password), ct).ConfigureAwait(false);

        if (!"success".Equals(gatewayResponse.Status, StringComparison.OrdinalIgnoreCase))
        {
            return LoginResult.InvalidCredentials;
        }

        var email = gatewayResponse.Email!;
        var user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);

        if (user is null)
        {
            user = User.CreateStubFromAd(
                email,
                gatewayResponse.FirstName,
                gatewayResponse.LastName,
                gatewayResponse.DisplayName);

            var createResult = await _userManager.CreateAsync(user).ConfigureAwait(false);
            if (!createResult.Succeeded)
            {
                return LoginResult.InvalidCredentials;
            }
        }

        // Deactivated accounts cannot sign in via the admin/AD path either.
        if (user.Status != UserStatus.Active)
            return LoginResult.Deactivated;

        await SyncAdRolesAsync(user, gatewayResponse.Groups).ConfigureAwait(false);

        var token = await IssueAndBuildDtoAsync(user, LocalAuthApi.Internal, ip, userAgent, null, ct).ConfigureAwait(false);
        return LoginResult.Success(token);
    }

    private async Task SyncAdRolesAsync(User user, IReadOnlyList<string>? adGroups)
    {
        if (adGroups is null || adGroups.Count == 0)
        {
            return;
        }

        var currentRoles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var desiredRoles = adGroups
            .Select(static g => AdRoleMapper.ToCceRole(g))
            .OfType<string>()
            .Distinct()
            .ToList();

        var rolesToAdd = desiredRoles.Except(currentRoles).ToList();
        var rolesToRemove = currentRoles.Except(desiredRoles).ToList();

        foreach (var role in rolesToAdd)
        {
            if (!await _userManager.IsInRoleAsync(user, role!).ConfigureAwait(false))
            {
                await _userManager.AddToRoleAsync(user, role!).ConfigureAwait(false);
            }
        }

        foreach (var role in rolesToRemove)
        {
            await _userManager.RemoveFromRoleAsync(user, role).ConfigureAwait(false);
        }
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
