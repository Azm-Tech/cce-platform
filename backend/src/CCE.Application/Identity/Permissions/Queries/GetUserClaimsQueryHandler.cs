using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Domain;
using MediatR;

namespace CCE.Application.Identity.Permissions.Queries;

internal sealed class GetUserClaimsQueryHandler
    : IRequestHandler<GetUserClaimsQuery, Response<UserClaimsListDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetUserClaimsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<UserClaimsListDto>> Handle(
        GetUserClaimsQuery request, CancellationToken cancellationToken)
    {
        var userExists = await _db.Users
            .AnyAsyncEither(u => u.Id == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (!userExists)
            return _msg.NotFound<UserClaimsListDto>("USER_NOT_FOUND");

        var claims = await _db.UserClaims
            .Where(uc => uc.UserId == request.UserId && uc.ClaimValue != null)
            .Select(uc => uc.ClaimValue!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var updatedAt = await _db.PermissionAuditLogs
            .Select(l => (DateTimeOffset?)l.ChangedAtUtc)
            .MaxAsyncEither(cancellationToken)
            .ConfigureAwait(false) ?? DateTimeOffset.MinValue;

        var items = claims
            .Select(c => new UserClaimItemDto(c, GetPermissionsQueryHandler.DeriveDisplayName(c)))
            .ToArray();

        return _msg.Ok(new UserClaimsListDto(request.UserId, items, updatedAt), "ITEMS_LISTED");
    }
}
