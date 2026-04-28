using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Commands.AssignUserRoles;

/// <summary>
/// Replaces the role assignments for the user with the given set of role names.
/// User entities don't carry RowVersion; concurrency is left out by design (single-operator
/// admin tooling). Phase 1.x can revisit if multi-admin contention becomes a real risk.
/// </summary>
public sealed record AssignUserRolesCommand(
    Guid Id,
    IReadOnlyList<string> Roles) : IRequest<UserDetailDto?>;
