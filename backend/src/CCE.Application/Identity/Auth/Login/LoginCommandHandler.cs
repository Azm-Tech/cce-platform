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
            LoginFailureReason.Deactivated => _msg.AccountDeactivated<AuthTokenDto>(),
            LoginFailureReason.ContactNotVerified => _msg.ContactNotVerified<AuthTokenDto>(),
            LoginFailureReason.None => _msg.Ok(result.Token!, MessageKeys.Identity.LOGIN_SUCCESS),
            _ => _msg.InvalidCredentials<AuthTokenDto>(),
        };
    }
}
