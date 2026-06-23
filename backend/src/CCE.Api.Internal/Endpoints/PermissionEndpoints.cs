using CCE.Api.Common.Extensions;
using CCE.Application.Identity.Permissions.Commands;
using CCE.Application.Identity.Permissions.Queries;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class PermissionEndpoints
{
    public static IEndpointRouteBuilder MapPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/permissions").WithTags("Permissions");

        group.MapGet("", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPermissionsQuery(), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Permission_Read)
        .WithName("ListPermissions")
        .WithSummary("List all permissions grouped by feature area")
        .WithDescription("Returns every known permission organised by group (the first dot-segment). " +
                         "Use this to populate the rows of a role-permission matrix UI.");

        group.MapGet("/matrix", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPermissionMatrixQuery(), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Permission_Read)
        .WithName("GetPermissionMatrix")
        .WithSummary("Full role-permission boolean matrix")
        .WithDescription("Returns all permissions grouped by feature area. " +
                         "Each permission carries a per-role boolean indicating whether the role currently holds it. " +
                         "Toggle a cell → call PUT /api/admin/roles/{role}/permissions with the updated permission list.");

        var roles = app.MapGroup("/api/admin/roles").WithTags("Permissions");

        roles.MapPut("/{role}/permissions", async (
            string role,
            UpsertRolePermissionsRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var permissions = (body.Permissions ?? [])
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToHashSet(StringComparer.Ordinal);

            var cmd = new UpsertRolePermissionsCommand(role, permissions);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Permission_Manage)
        .WithName("UpsertRolePermissions")
        .WithSummary("Replace all permissions for a role (full upsert)")
        .WithDescription(
            """
            Replaces the complete permission set for the given role in one atomic operation.
            Send the FULL desired list — permissions absent from the list are revoked.

            Example request body:
            {
              "permissions": [
                "community.post.create",
                "community.post.reply",
                "community.post.vote",
                "news.publish",
                "news.update"
              ]
            }

            To remove all permissions from a role, send: { "permissions": [] }
            """);

        roles.MapPost("/{role}/permissions/grant", async (
            string role,
            GrantRolePermissionsRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var permissions = (body.Permissions ?? [])
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToHashSet(StringComparer.Ordinal);

            var cmd = new GrantRolePermissionsCommand(role, permissions);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Permission_Manage)
        .WithName("GrantRolePermissions")
        .WithSummary("Grant permissions to a role (additive)")
        .WithDescription(
            """
            Adds the specified permissions to the role's existing set.
            Permissions the role already holds are left unchanged.

            Example request body:
            {
              "permissions": ["community.post.vote", "news.publish"]
            }
            """);

        roles.MapPost("/{role}/permissions/revoke", async (
            string role,
            RevokeRolePermissionsRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var permissions = (body.Permissions ?? [])
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToHashSet(StringComparer.Ordinal);

            var cmd = new RevokeRolePermissionsCommand(role, permissions);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Permission_Manage)
        .WithName("RevokeRolePermissions")
        .WithSummary("Revoke permissions from a role (subtractive)")
        .WithDescription(
            """
            Removes the specified permissions from the role's existing set.
            Permissions not held by the role are ignored.

            Example request body:
            {
              "permissions": ["news.publish"]
            }
            """);

        return app;
    }
}
