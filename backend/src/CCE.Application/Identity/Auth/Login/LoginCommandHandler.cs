using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Auth.Login;

internal sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, Response<AuthTokenDto>>
{
    private readonly IAuthService _auth;
    private readonly MessageFactory _msg;

    public LoginCommandHandler(IAuthService auth, MessageFactory msg)
    {
        _auth = auth;
        _msg = msg;
    }

    public async Task<Response<AuthTokenDto>> Handle(LoginCommand request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request.EmailAddress, request.Password, request.Api,
            request.IpAddress, request.UserAgent, ct).ConfigureAwait(false);
        return result.Failure switch
        {
            LoginFailureReason.Deactivated => _msg.Forbidden<AuthTokenDto>(MessageKeys.Identity.ACCOUNT_DEACTIVATED),
            LoginFailureReason.ContactNotVerified => _msg.Forbidden<AuthTokenDto>(MessageKeys.Identity.CONTACT_NOT_VERIFIED),
            LoginFailureReason.None => _msg.Ok(result.Token!, MessageKeys.Identity.LOGIN_SUCCESS),
            _ => _msg.Unauthorized<AuthTokenDto>(MessageKeys.Identity.INVALID_CREDENTIALS),
        };
    }
}
