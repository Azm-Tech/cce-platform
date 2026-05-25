using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Auth.Logout;

internal sealed class LogoutCommandHandler
    : IRequestHandler<LogoutCommand, Response<AuthMessageDto>>
{
    private readonly IAuthService _auth;
    private readonly MessageFactory _msg;

    public LogoutCommandHandler(IAuthService auth, MessageFactory msg)
    {
        _auth = auth;
        _msg = msg;
    }

    public async Task<Response<AuthMessageDto>> Handle(LogoutCommand request, CancellationToken ct)
    {
        await _auth.LogoutAsync(request.RefreshToken, request.IpAddress, ct).ConfigureAwait(false);
        return _msg.Ok(new AuthMessageDto("LOGOUT_SUCCESS"), "LOGOUT_SUCCESS");
    }
}
