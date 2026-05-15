using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using AppErrors = CCE.Application.Common.Errors;

namespace CCE.Application.Identity.Auth.Login;

internal sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, Result<AuthTokenDto>>
{
    private readonly UserManager<User> _userManager;
    private readonly ILocalTokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ISystemClock _clock;
    private readonly IOptions<LocalAuthOptions> _options;
    private readonly AppErrors _errors;

    public LoginCommandHandler(
        UserManager<User> userManager,
        ILocalTokenService tokenService,
        IRefreshTokenRepository refreshTokens,
        ISystemClock clock,
        IOptions<LocalAuthOptions> options,
        AppErrors errors)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _clock = clock;
        _options = options;
        _errors = errors;
    }

    public async Task<Result<AuthTokenDto>> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(request.EmailAddress).ConfigureAwait(false);
        if (user is null)
        {
            return _errors.InvalidCredentials();
        }

        if (_options.Value.RequireConfirmedEmail && !await _userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false))
        {
            return _errors.InvalidCredentials();
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password).ConfigureAwait(false);
        if (!passwordValid)
        {
            return _errors.InvalidCredentials();
        }

        return await IssueAndPersistAsync(user, request.Api, request.IpAddress, request.UserAgent, null, ct).ConfigureAwait(false);
    }

    private async Task<AuthTokenDto> IssueAndPersistAsync(
        User user,
        LocalAuthApi api,
        string? ipAddress,
        string? userAgent,
        Guid? tokenFamilyId,
        CancellationToken ct)
    {
        var issued = await _tokenService.IssueAsync(user, api, ct).ConfigureAwait(false);
        var familyId = tokenFamilyId ?? Guid.NewGuid();
        var refreshToken = CCE.Domain.Identity.RefreshToken.Create(
            user.Id,
            issued.RefreshTokenHash,
            familyId,
            _clock.UtcNow,
            issued.RefreshTokenExpiresAtUtc,
            ipAddress,
            userAgent);
        await _refreshTokens.AddAsync(refreshToken, ct).ConfigureAwait(false);
        await _refreshTokens.SaveChangesAsync(ct).ConfigureAwait(false);
        return await ToDtoAsync(user, issued, ct).ConfigureAwait(false);
    }

    private async Task<AuthTokenDto> ToDtoAsync(User user, TokenIssueResult issued, CancellationToken ct)
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
