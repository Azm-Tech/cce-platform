using CCE.Application.Common;
using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Commands.AssignUserRoles;

/// <summary>
/// Replaces the role assignments for the user with the given set of role names.
/// </summary>
public sealed record AssignUserRolesCommand(
    Guid Id,
    IReadOnlyList<string> Roles) : IRequest<Response<UserDetailDto>>;
