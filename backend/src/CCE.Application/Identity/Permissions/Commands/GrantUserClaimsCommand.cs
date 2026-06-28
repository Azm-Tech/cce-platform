using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Identity.Permissions.Commands;

public sealed record GrantUserClaimsRequest(IReadOnlyList<string> Claims);

public sealed record GrantUserClaimsCommand(
    Guid UserId,
    IReadOnlySet<string> Claims) : IRequest<Response<UserClaimsResult>>;
