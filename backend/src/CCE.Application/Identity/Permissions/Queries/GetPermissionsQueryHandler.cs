using System.Globalization;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Permissions.Queries;

internal sealed class GetPermissionsQueryHandler
    : IRequestHandler<GetPermissionsQuery, Response<PermissionsListDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetPermissionsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PermissionsListDto>> Handle(
        GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var names = await _db.RoleClaims
            .Where(rc => rc.ClaimType == "permission" && rc.ClaimValue != null)
            .Select(rc => rc.ClaimValue!)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var updatedAt = await _db.PermissionAuditLogs
            .Select(l => (DateTimeOffset?)l.ChangedAtUtc)
            .MaxAsyncEither(cancellationToken)
            .ConfigureAwait(false) ?? DateTimeOffset.MinValue;

        var groups = names
            .GroupBy(FirstSegment)
            .OrderBy(g => g.Key)
            .Select(g => new PermissionGroupDto(
                ToTitle(g.Key),
                g.Select(claim => new PermissionItemDto(claim, DeriveDisplayName(claim))).ToArray()))
            .ToArray();

        return _msg.Ok(new PermissionsListDto(groups, updatedAt), MessageKeys.General.ITEMS_LISTED);
    }

    internal static string FirstSegment(string claim)
    {
        var dot = claim.IndexOf('.', StringComparison.Ordinal);
        return dot > 0 ? claim[..dot] : claim;
    }

    internal static string DeriveDisplayName(string claim)
    {
        var parts = claim.Split('.');
        if (parts.Length < 2) return ToTitle(claim);
        var subParts = parts[1..];
        if (subParts.Length == 1) return ToTitle(subParts[0]);
        var verb = ToTitle(subParts[^1]);
        var context = string.Join(" ", subParts[..^1].Select(ToTitle));
        return $"{verb} {context}";
    }

    private static string ToTitle(string segment)
        => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(segment);
}