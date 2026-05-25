using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using MediatR;

namespace CCE.Application.Identity.Auth.Logout;

public sealed record LogoutCommand(string RefreshToken, string? IpAddress)
    : IRequest<Response<AuthMessageDto>>;
