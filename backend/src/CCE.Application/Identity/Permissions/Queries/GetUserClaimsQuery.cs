using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Identity.Permissions.Queries;

public sealed record UserClaimItemDto(string Claim, string DisplayName);

public sealed record UserClaimsListDto(
    Guid UserId,
    IReadOnlyList<UserClaimItemDto> Claims,
    DateTimeOffset UpdatedAt);

public sealed record GetUserClaimsQuery(Guid UserId) : IRequest<Response<UserClaimsListDto>>;
