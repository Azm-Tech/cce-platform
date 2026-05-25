using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Auth.RefreshToken;

internal sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, Response<AuthTokenDto>>
{
    private readonly IAuthService _auth;
    private readonly MessageFactory _msg;

    public RefreshTokenCommandHandler(IAuthService auth, MessageFactory msg)
    {
        _auth = auth;
        _msg = msg;
    }

    public async Task<Response<AuthTokenDto>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var dto = await _auth.RefreshTokenAsync(request.RefreshToken, request.Api,
            request.IpAddress, request.UserAgent, ct).ConfigureAwait(false);
        if (dto is null) return _msg.Unauthorized<AuthTokenDto>("INVALID_REFRESH_TOKEN");
        return _msg.Ok(dto, "TOKEN_REFRESHED");
    }
}
