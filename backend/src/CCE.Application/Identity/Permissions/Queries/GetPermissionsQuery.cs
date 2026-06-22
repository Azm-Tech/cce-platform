using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Identity.Permissions.Queries;

public sealed record PermissionItemDto(string Claim, string DisplayName);

public sealed record PermissionGroupDto(string Name, IReadOnlyList<PermissionItemDto> Permissions);

public sealed record PermissionsListDto(
    IReadOnlyList<PermissionGroupDto> Groups,
    DateTimeOffset UpdatedAt);

public sealed record GetPermissionsQuery : IRequest<Response<PermissionsListDto>>;
