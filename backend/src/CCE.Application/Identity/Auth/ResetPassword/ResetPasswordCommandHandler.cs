using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using AppErrorCodes = CCE.Application.Errors.ApplicationErrors;
using AppErrors = CCE.Application.Common.Errors;

namespace CCE.Application.Identity.Auth.ResetPassword;

internal sealed class ResetPasswordCommandHandler
    : IRequestHandler<ResetPasswordCommand, Result<AuthMessageDto>>
{
    private readonly UserManager<User> _userManager;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ISystemClock _clock;
    private readonly AppErrors _errors;

    public ResetPasswordCommandHandler(
        UserManager<User> userManager,
        IRefreshTokenRepository refreshTokens,
        ISystemClock clock,
        AppErrors errors)
    {
        _userManager = userManager;
        _refreshTokens = refreshTokens;
        _clock = clock;
        _errors = errors;
    }

    public async Task<Result<AuthMessageDto>> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(request.EmailAddress).ConfigureAwait(false);
        if (user is null)
        {
            return _errors.UserNotFound();
        }

        string token;
        try
        {
            token = PasswordResetTokenCodec.Decode(request.Token);
        }
        catch (FormatException)
        {
            return _errors.InvalidRefreshToken();
        }

        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return _errors.RegistrationFailed(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["Identity"] = result.Errors.Select(e => e.Code).ToArray(),
            });
        }

        await _userManager.UpdateSecurityStampAsync(user).ConfigureAwait(false);
        await _refreshTokens.RevokeAllForUserAsync(user.Id, _clock.UtcNow, request.IpAddress, ct).ConfigureAwait(false);
        await _refreshTokens.SaveChangesAsync(ct).ConfigureAwait(false);
        return new AuthMessageDto(AppErrorCodes.Identity.PASSWORD_RESET);
    }
}
