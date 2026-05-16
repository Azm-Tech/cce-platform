using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using MediatR;

namespace CCE.Application.Identity.Auth.ResetPassword;

public sealed record ResetPasswordCommand(
    string EmailAddress,
    string Token,
    string NewPassword,
    string ConfirmPassword,
    string? IpAddress)
    : IRequest<Response<AuthMessageDto>>;
