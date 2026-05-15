using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using CCE.Domain.Common;
using MediatR;
using AppErrorCodes = CCE.Application.Errors.ApplicationErrors;

namespace CCE.Application.Identity.Auth.Logout;

internal sealed class LogoutCommandHandler
    : IRequestHandler<LogoutCommand, Result<AuthMessageDto>>
{
    private readonly ILocalTokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ISystemClock _clock;

    public LogoutCommandHandler(
        ILocalTokenService tokenService,
        IRefreshTokenRepository refreshTokens,
        ISystemClock clock)
    {
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _clock = clock;
    }

    public async Task<Result<AuthMessageDto>> Handle(LogoutCommand request, CancellationToken ct)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var existing = await _refreshTokens.FindByHashAsync(tokenHash, ct).ConfigureAwait(false);
        if (existing is not null && existing.IsActive(_clock.UtcNow))
        {
            existing.Revoke(_clock.UtcNow, request.IpAddress);
            await _refreshTokens.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return new AuthMessageDto(AppErrorCodes.Identity.LOGOUT_SUCCESS);
    }
}
