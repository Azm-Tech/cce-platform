using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using AppErrors = CCE.Application.Common.Errors;

namespace CCE.Application.Identity.Auth.RefreshToken;

internal sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, Result<AuthTokenDto>>
{
    private readonly UserManager<User> _userManager;
    private readonly ILocalTokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ISystemClock _clock;
    private readonly AppErrors _errors;

    public RefreshTokenCommandHandler(
        UserManager<User> userManager,
        ILocalTokenService tokenService,
        IRefreshTokenRepository refreshTokens,
        ISystemClock clock,
        AppErrors errors)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _clock = clock;
        _errors = errors;
    }

    public async Task<Result<AuthTokenDto>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var existing = await _refreshTokens.FindByHashAsync(tokenHash, ct).ConfigureAwait(false);
        if (existing is null)
        {
            return _errors.InvalidRefreshToken();
        }

        if (!existing.IsActive(_clock.UtcNow))
        {
            if (existing.RevokedAtUtc is not null)
            {
                await _refreshTokens.RevokeFamilyAsync(existing.TokenFamilyId, _clock.UtcNow, request.IpAddress, ct)
                    .ConfigureAwait(false);
                await _refreshTokens.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            return _errors.InvalidRefreshToken();
        }

        var user = await _userManager.FindByIdAsync(existing.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return _errors.InvalidRefreshToken();
        }

        var issued = await _tokenService.IssueAsync(user, request.Api, ct).ConfigureAwait(false);
        existing.Revoke(_clock.UtcNow, request.IpAddress, issued.RefreshTokenHash);

        var replacement = CCE.Domain.Identity.RefreshToken.Create(
            user.Id,
            issued.RefreshTokenHash,
            existing.TokenFamilyId,
            _clock.UtcNow,
            issued.RefreshTokenExpiresAtUtc,
            request.IpAddress,
            request.UserAgent);
        await _refreshTokens.AddAsync(replacement, ct).ConfigureAwait(false);
        await _refreshTokens.SaveChangesAsync(ct).ConfigureAwait(false);

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
