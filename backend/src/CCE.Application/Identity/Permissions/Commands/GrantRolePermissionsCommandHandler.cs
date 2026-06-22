using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Auth.Common;
using CCE.Application.Messages;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Identity.Permissions.Commands;

internal sealed class GrantRolePermissionsCommandHandler
    : IRequestHandler<GrantRolePermissionsCommand, Response<RolePermissionsResult>>
{
    private readonly IRolePermissionRepository _repo;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;
    private readonly IPermissionService _permissions;

    public GrantRolePermissionsCommandHandler(
        IRolePermissionRepository repo,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory msg,
        IPermissionService permissions)
    {
        _repo = repo;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
        _permissions = permissions;
    }

    public async Task<Response<RolePermissionsResult>> Handle(
        GrantRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetUserId() ?? Guid.Empty;
        var actorEmail = _currentUser.GetActor();

        var currentPerms = await _permissions
            .GetRolePermissionsAsync(request.RoleName, cancellationToken)
            .ConfigureAwait(false) ?? [];

        var merged = new HashSet<string>(currentPerms, StringComparer.Ordinal);
        merged.UnionWith(request.Permissions);

        var result = await _repo.UpsertAsync(
            request.RoleName, merged, actorId, actorEmail, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);

        return result is null
            ? _msg.NotFound<RolePermissionsResult>("ROLE_NOT_FOUND")
            : _msg.Ok(result, "PERMISSIONS_GRANTED");
    }
}
