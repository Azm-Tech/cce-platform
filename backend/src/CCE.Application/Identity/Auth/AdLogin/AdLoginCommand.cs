using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using MediatR;

namespace CCE.Application.Identity.Auth.AdLogin;

public sealed record AdLoginCommand(
    string Username,
    string Password,
    string? Ip,
    string? UserAgent)
    : IRequest<Response<AuthTokenDto>>;
