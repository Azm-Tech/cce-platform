using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using MediatR;

namespace CCE.Application.Identity.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(string EmailAddress)
    : IRequest<Response<AuthMessageDto>>;
