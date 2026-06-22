using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Identity.Permissions.Commands;

public sealed record UpsertUserClaimsRequest(IReadOnlyList<string> Claims);

public sealed record UpsertUserClaimsCommand(
    Guid UserId,
    IReadOnlySet<string> Claims) : IRequest<Response<UserClaimsResult>>;

public sealed record UserClaimsResult(
    Guid UserId,
    IReadOnlyList<string> Claims,
    int Granted,
    int Revoked,
    int Total);
