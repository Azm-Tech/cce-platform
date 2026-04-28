using CCE.Application.Identity.Queries.GetUserById;
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

        return app;
    }
}
