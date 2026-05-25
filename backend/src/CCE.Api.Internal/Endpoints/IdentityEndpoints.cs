using CCE.Api.Common.Extensions;
using CCE.Application.Identity.Commands.AssignUserRoles;
using CCE.Application.Identity.Commands.ChangeUserStatus;
using CCE.Application.Identity.Commands.CreateStateRepAssignment;
using CCE.Application.Identity.Commands.CreateUser;
using CCE.Application.Identity.Commands.DeleteUser;
using CCE.Application.Identity.Commands.RevokeStateRepAssignment;
using CCE.Application.Identity.Queries.GetUserById;
using CCE.Application.Identity.Queries.ListStateRepAssignments;
using CCE.Application.Identity.Queries.ListUsers;
using CCE.Domain;
using CCE.Infrastructure.Identity;
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
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.User_Read)
        .WithName("ListUsers");

        users.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetUserByIdQuery(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.User_Read)
        .WithName("GetUserById");

        users.MapPost("", async (
            CreateUserRequest body,
            IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new CreateUserCommand(
                body.FirstName, body.LastName, body.Email,
                body.PhoneNumber, body.CountryId, body.CountryCodeId, body.Role);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToCreatedHttpResult();
        })
        .RequireAuthorization(Permissions.User_Create)
        .WithName("CreateUser");

        users.MapPut("/{id:guid}/roles", async (
            System.Guid id,
            AssignUserRolesRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new AssignUserRolesCommand(id, body.Roles ?? System.Array.Empty<string>());
            var result = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Role_Assign)
        .WithName("AssignUserRoles");

        users.MapDelete("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteUserCommand(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.User_Delete)
        .WithName("DeleteUser");

        users.MapPut("/{id:guid}/status", async (
            System.Guid id,
            ChangeUserStatusRequest body,
            IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new ChangeUserStatusCommand(id, body.IsActive);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.User_Update)
        .WithName("ChangeUserStatus");

        // Sub-11d Task D — batch UPN→EntraIdObjectId backfill. Admin-only;
        // referenced by docs/runbooks/entra-id-cutover.md step 7. Lazy
        // resolution per-user already happens on first sign-in via
        // EntraIdUserResolver; this endpoint pre-populates the link in
        // bulk so cutover-day first-sign-ins are single-round-trip.
        users.MapPost("/sync", async (
            EntraIdUserSyncService syncService,
            CancellationToken ct) =>
        {
            var summary = await syncService.SyncAsync(ct).ConfigureAwait(false);
            return Results.Ok(summary);
        })
        .RequireAuthorization(Permissions.Role_Assign)
        .WithName("SyncEntraIdUsers");

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

        assignments.MapPost("", async (
            CreateStateRepAssignmentRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new CreateStateRepAssignmentCommand(body.UserId, body.CountryId);
            var result = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return result.ToCreatedHttpResult();
        })
        .RequireAuthorization(Permissions.Role_Assign)
        .WithName("CreateStateRepAssignment");

        assignments.MapDelete("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new RevokeStateRepAssignmentCommand(id), cancellationToken).ConfigureAwait(false);
            return result.ToNoContentHttpResult();
        })
        .RequireAuthorization(Permissions.Role_Assign)
        .WithName("RevokeStateRepAssignment");

        return app;
    }
}

public sealed record ChangeUserStatusRequest(bool IsActive);

public sealed record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    System.Guid? CountryId,
    System.Guid? CountryCodeId,
    string Role);