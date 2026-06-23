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

internal sealed class GrantUserClaimsCommandHandler
    : IRequestHandler<GrantUserClaimsCommand, Response<UserClaimsResult>>
{
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;
    private readonly IPermissionService _permissions;

    public GrantUserClaimsCommandHandler(
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
        GrantUserClaimsCommand request, CancellationToken cancellationToken)
    {
        var userExists = await _db.Users
            .AnyAsyncEither(u => u.Id == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (!userExists)
            return _msg.NotFound<UserClaimsResult>("USER_NOT_FOUND");

        var actorId = _currentUser.GetUserId() ?? Guid.Empty;
        var actorEmail = _currentUser.GetActor();
        var now = _clock.UtcNow;

        var existingList = await _db.UserClaims
            .Where(uc => uc.UserId == request.UserId && uc.ClaimValue != null)
            .Select(uc => uc.ClaimValue!)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var existing = new HashSet<string>(existingList, StringComparer.Ordinal);

        var toAdd = request.Claims.Except(existing).ToList();

        foreach (var claim in toAdd)
        {
            _db.Add(new IdentityUserClaim<Guid>
            {
                UserId = request.UserId,
                ClaimType = "permission",
                ClaimValue = claim,
            });
            _db.Add(PermissionAuditLog.Record(now, actorId, actorEmail,
                $"user:{request.UserId}", claim, PermissionAuditAction.Granted));
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _permissions.InvalidateCacheForUser(request.UserId);

        var all = new HashSet<string>(existing, StringComparer.Ordinal);
        all.UnionWith(request.Claims);

        return _msg.Ok(new UserClaimsResult(
            request.UserId,
            all.OrderBy(c => c).ToArray(),
            toAdd.Count,
            0,
            all.Count), "CLAIMS_GRANTED");
    }
}
