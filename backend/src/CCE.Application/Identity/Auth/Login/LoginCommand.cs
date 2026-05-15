using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using MediatR;

namespace CCE.Application.Identity.Auth.Login;

public sealed record LoginCommand(
    string EmailAddress,
    string Password,
    LocalAuthApi Api,
    string? IpAddress,
    string? UserAgent)
    : IRequest<Result<AuthTokenDto>>;
