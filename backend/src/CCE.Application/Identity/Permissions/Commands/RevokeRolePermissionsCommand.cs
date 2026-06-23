using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Identity.Permissions.Commands;

public sealed record RevokeRolePermissionsRequest(IReadOnlyList<string> Permissions);

public sealed record RevokeRolePermissionsCommand(
    string RoleName,
    IReadOnlySet<string> Permissions) : IRequest<Response<RolePermissionsResult>>;
