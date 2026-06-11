using CCE.Application.Common;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetCommunityRoles;

/// <summary>Returns the fixed community membership role definitions (Member, Moderator).</summary>
public sealed record GetCommunityRolesQuery
    : IRequest<Response<System.Collections.Generic.IReadOnlyList<CommunityRoleDto>>>;
