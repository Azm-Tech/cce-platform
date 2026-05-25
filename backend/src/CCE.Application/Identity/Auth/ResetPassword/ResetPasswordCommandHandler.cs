using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Auth.ResetPassword;

internal sealed class ResetPasswordCommandHandler
    : IRequestHandler<ResetPasswordCommand, Response<AuthMessageDto>>
{
    private readonly IAuthService _auth;
    private readonly MessageFactory _msg;

    public ResetPasswordCommandHandler(IAuthService auth, MessageFactory msg)
    {
        _auth = auth;
        _msg = msg;
    }

    public async Task<Response<AuthMessageDto>> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var errorKey = await _auth.ResetPasswordAsync(request.EmailAddress, request.Token,
            request.NewPassword, request.IpAddress, ct).ConfigureAwait(false);

        if (errorKey is not null)
        {
            return errorKey switch
            {
                "USER_NOT_FOUND" => _msg.UserNotFound<AuthMessageDto>(),
                "INVALID_RESET_TOKEN" => _msg.Unauthorized<AuthMessageDto>("INVALID_RESET_TOKEN"),
                _ => _msg.BusinessRule<AuthMessageDto>(errorKey),
            };
        }

        return _msg.Ok(new AuthMessageDto("PASSWORD_RESET"), "PASSWORD_RESET");
    }
}
