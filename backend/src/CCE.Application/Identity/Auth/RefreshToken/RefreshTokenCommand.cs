using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using MediatR;

namespace CCE.Application.Identity.Auth.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    LocalAuthApi Api,
    string? IpAddress,
    string? UserAgent)
    : IRequest<Response<AuthTokenDto>>;
