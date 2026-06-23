using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Identity.Permissions.Commands;

public sealed record RevokeUserClaimsRequest(IReadOnlyList<string> Claims);

public sealed record RevokeUserClaimsCommand(
    Guid UserId,
    IReadOnlySet<string> Claims) : IRequest<Response<UserClaimsResult>>;
