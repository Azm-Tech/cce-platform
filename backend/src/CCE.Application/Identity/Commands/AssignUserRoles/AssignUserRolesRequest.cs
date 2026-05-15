namespace CCE.Application.Identity.Commands.AssignUserRoles;

public sealed record AssignUserRolesRequest(IReadOnlyList<string>? Roles);
