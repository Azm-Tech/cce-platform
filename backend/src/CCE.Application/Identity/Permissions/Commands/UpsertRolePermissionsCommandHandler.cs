using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Identity.Permissions.Commands;

internal sealed class UpsertRolePermissionsCommandHandler
    : IRequestHandler<UpsertRolePermissionsCommand, Response<RolePermissionsResult>>
{
    private readonly IRolePermissionRepository _repo;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public UpsertRolePermissionsCommandHandler(
        IRolePermissionRepository repo,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory msg)
    {
        _repo = repo;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<RolePermissionsResult>> Handle(
        UpsertRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var actorId    = _currentUser.GetUserId() ?? Guid.Empty;
        var actorEmail = _currentUser.GetActor();

        var result = await _repo.UpsertAsync(
            request.RoleName,
            request.Permissions,
            actorId,
            actorEmail,
            _clock.UtcNow,
            cancellationToken).ConfigureAwait(false);

        return result is null
            ? _msg.NotFound<RolePermissionsResult>(MessageKeys.Identity.ROLE_NOT_FOUND)
            : _msg.Ok(result, MessageKeys.Identity.PERMISSIONS_UPDATED);
    }
}
