using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Auth.AdLogin;

internal sealed class AdLoginCommandHandler
    : IRequestHandler<AdLoginCommand, Response<AuthTokenDto>>
{
    private readonly IAuthService _auth;
    private readonly MessageFactory _msg;

    public AdLoginCommandHandler(IAuthService auth, MessageFactory msg)
    {
        _auth = auth;
        _msg = msg;
    }

    public async Task<Response<AuthTokenDto>> Handle(AdLoginCommand request, CancellationToken ct)
    {
        var result = await _auth.AdLoginAsync(
            request.Username,
            request.Password,
            request.Ip,
            request.UserAgent,
            ct).ConfigureAwait(false);

        return result.Failure switch
        {
            LoginFailureReason.Deactivated => _msg.AccountDeactivated<AuthTokenDto>(),
            LoginFailureReason.None => _msg.Ok(result.Token!, MessageKeys.Identity.AD_LOGIN_SUCCESS),
            _ => _msg.InvalidCredentials<AuthTokenDto>(),
        };
    }
}
