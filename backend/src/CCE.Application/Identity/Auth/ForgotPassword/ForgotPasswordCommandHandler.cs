using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Auth.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler
    : IRequestHandler<ForgotPasswordCommand, Response<AuthMessageDto>>
{
    private readonly IAuthService _auth;
    private readonly MessageFactory _msg;

    public ForgotPasswordCommandHandler(IAuthService auth, MessageFactory msg)
    {
        _auth = auth;
        _msg = msg;
    }

    public async Task<Response<AuthMessageDto>> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        await _auth.ForgotPasswordAsync(request.EmailAddress, ct).ConfigureAwait(false);
        return _msg.Ok(new AuthMessageDto("PASSWORD_RESET"), "PASSWORD_RESET");
    }
}
