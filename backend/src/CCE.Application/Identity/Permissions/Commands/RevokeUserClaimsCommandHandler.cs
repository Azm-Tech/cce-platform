using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Auth.Common;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CCE.Application.Identity.Permissions.Commands;

internal sealed class RevokeUserClaimsCommandHandler
    : IRequestHandler<RevokeUserClaimsCommand, Response<UserClaimsResult>>
{
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;
    private readonly IPermissionService _permissions;

    public RevokeUserClaimsCommandHandler(
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory msg,
        IPermissionService permissions)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
        _permissions = permissions;
    }

    public async Task<Response<UserClaimsResult>> Handle(
        RevokeUserClaimsCommand request, CancellationToken cancellationToken)
    {
        var userExists = await _db.Users
            .AnyAsyncEither(u => u.Id == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (!userExists)
            return _msg.NotFound<UserClaimsResult>(MessageKeys.Identity.USER_NOT_FOUND);

        var actorId = _currentUser.GetUserId() ?? Guid.Empty;
        var actorEmail = _currentUser.GetActor();
        var now = _clock.UtcNow;

        var toRemove = await _db.UserClaims
            .Where(uc => uc.UserId == request.UserId
                         && uc.ClaimValue != null
                         && request.Claims.Contains(uc.ClaimValue!))
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        foreach (var uc in toRemove)
        {
            _db.Delete(uc);
            _db.Add(PermissionAuditLog.Record(now, actorId, actorEmail,
                $"user:{request.UserId}", uc.ClaimValue!, PermissionAuditAction.Revoked));
        }

        var existing = await _db.UserClaims
            .Where(uc => uc.UserId == request.UserId && uc.ClaimValue != null)
            .Select(uc => uc.ClaimValue!)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _permissions.InvalidateCacheForUser(request.UserId);

        return _msg.Ok(new UserClaimsResult(
            request.UserId,
            [.. existing.OrderBy(c => c)],
            0,
            toRemove.Count,
            existing.Count), MessageKeys.Identity.CLAIMS_REVOKED);
    }
}
