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
        var dto = await _auth.AdLoginAsync(
            request.Username,
            request.Password,
            request.Ip,
            request.UserAgent,
            ct).ConfigureAwait(false);

        if (dto is null)
        {
            return _msg.InvalidCredentials<AuthTokenDto>();
        }

        return _msg.Ok(dto, "AD_LOGIN_SUCCESS");
    }
}
