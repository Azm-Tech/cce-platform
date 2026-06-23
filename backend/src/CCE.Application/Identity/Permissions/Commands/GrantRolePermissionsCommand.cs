using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Identity.Permissions.Commands;

public sealed record GrantRolePermissionsRequest(IReadOnlyList<string> Permissions);

public sealed record GrantRolePermissionsCommand(
    string RoleName,
    IReadOnlySet<string> Permissions) : IRequest<Response<RolePermissionsResult>>;
