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
        var dto = await _auth.LoginAsync(request.EmailAddress, request.Password, request.Api,
            request.IpAddress, request.UserAgent, ct).ConfigureAwait(false);
        if (dto is null) return _msg.InvalidCredentials<AuthTokenDto>();
        return _msg.Ok(dto, "LOGIN_SUCCESS");
    }
}
