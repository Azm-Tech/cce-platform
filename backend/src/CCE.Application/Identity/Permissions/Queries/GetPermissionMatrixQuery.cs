using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Identity.Permissions.Queries;

public sealed record PermissionMatrixItemDto(string Claim, IReadOnlyList<bool> Grants);

public sealed record PermissionMatrixGroupDto(string Name, IReadOnlyList<PermissionMatrixItemDto> Permissions);

public sealed record PermissionMatrixDto(
    IReadOnlyList<string> Roles,
    IReadOnlyList<PermissionMatrixGroupDto> Entities,
    DateTimeOffset UpdatedAt);

public sealed record GetPermissionMatrixQuery : IRequest<Response<PermissionMatrixDto>>;
