using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Domain;
using MediatR;

namespace CCE.Application.Identity.Permissions.Queries;

internal sealed class GetPermissionMatrixQueryHandler
    : IRequestHandler<GetPermissionMatrixQuery, Response<PermissionMatrixDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetPermissionMatrixQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PermissionMatrixDto>> Handle(
        GetPermissionMatrixQuery request, CancellationToken cancellationToken)
    {
        var dbRoleNames = await _db.Roles
            .Where(r => r.Name != null)
            .Select(r => r.Name!)
            .OrderBy(n => n)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var roles = dbRoleNames.Concat(["Anonymous"]).ToArray();

        var rawAssignments = await (
            from rc in _db.RoleClaims
            join r in _db.Roles on rc.RoleId equals r.Id
            where rc.ClaimType == "permission"
               && rc.ClaimValue != null
               && r.Name != null
            select new { RoleName = r.Name!, Permission = rc.ClaimValue! }
        ).ToListAsyncEither(cancellationToken).ConfigureAwait(false);

        var grantedRolesByPermission = rawAssignments
            .GroupBy(x => x.Permission)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToHashSet());

        // Merge Anonymous permissions from the compile-time RolePermissionMap
        var anonymousPerms = RolePermissionMap.Anonymous.ToHashSet();
        foreach (var perm in anonymousPerms)
        {
            if (!grantedRolesByPermission.TryGetValue(perm, out var set))
            {
                set = [];
                grantedRolesByPermission[perm] = set;
            }
            set.Add("Anonymous");
        }

        var updatedAt = await _db.PermissionAuditLogs
            .Select(l => (DateTimeOffset?)l.ChangedAtUtc)
            .MaxAsyncEither(cancellationToken)
            .ConfigureAwait(false) ?? DateTimeOffset.MinValue;

        var entities = grantedRolesByPermission.Keys
            .OrderBy(p => p)
            .GroupBy(GetPermissionsQueryHandler.FirstSegment)
            .OrderBy(g => g.Key)
            .Select(g => new PermissionMatrixGroupDto(
                ToTitle(g.Key),
                g.Select(claim =>
                {
                    var granted = grantedRolesByPermission.TryGetValue(claim, out var rolesForPerm)
                        ? rolesForPerm
                        : [];
                    var grants = roles.Select(r => granted.Contains(r)).ToArray();
                    return new PermissionMatrixItemDto(claim, grants);
                }).ToArray()))
            .ToArray();

        return _msg.Ok(new PermissionMatrixDto(roles, entities, updatedAt), "ITEMS_LISTED");
    }

    private static string ToTitle(string segment)
        => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(segment);
}