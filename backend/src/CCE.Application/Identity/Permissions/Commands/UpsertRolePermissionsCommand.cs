using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Identity.Permissions.Commands;

public sealed record UpsertRolePermissionsRequest(IReadOnlyList<string>? Permissions);

public sealed record UpsertRolePermissionsCommand(
    string RoleName,
    IReadOnlySet<string> Permissions) : IRequest<Response<RolePermissionsResult>>;
