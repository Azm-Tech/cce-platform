using CCE.Application.Identity.Commands.AssignUserRoles;
using CCE.Application.Identity.Queries.GetUserById;
using CCE.Application.Identity.Queries.ListStateRepAssignments;
using CCE.Application.Identity.Queries.ListUsers;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

/// <summary>
/// Maps the Identity-area admin endpoints under <c>/api/admin/users</c> and
/// <c>/api/admin/state-rep-assignments</c>.
/// </summary>
public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/admin/users").WithTags("Identity");

        users.MapGet("", async (
            int? page, int? pageSize, string? search, string? role,
            IMediator mediator, CancellationToken ct) =>
        {
            var query = new ListUsersQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                Search: search,
                Role: role);
            var result = await mediator.Send(query, ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.User_Read)
        .WithName("ListUsers");

        users.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetUserByIdQuery(id), ct).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.User_Read)
        .WithName("GetUserById");

        users.MapPut("/{id:guid}/roles", async (
            System.Guid id,
            AssignUserRolesRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new AssignUserRolesCommand(id, body.Roles ?? System.Array.Empty<string>());
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Role_Assign)
        .WithName("AssignUserRoles");

        var assignments = app.MapGroup("/api/admin/state-rep-assignments").WithTags("Identity");

        assignments.MapGet("", async (
            int? page, int? pageSize, System.Guid? userId, System.Guid? countryId, bool? active,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListStateRepAssignmentsQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                UserId: userId,
                CountryId: countryId,
                Active: active ?? true);
            var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.Role_Assign)
        .WithName("ListStateRepAssignments");

        return app;
    }
}

/// <summary>Body shape for PUT /api/admin/users/{id}/roles.</summary>
public sealed record AssignUserRolesRequest(IReadOnlyList<string>? Roles);
