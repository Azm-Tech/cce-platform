using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Public.Queries.GetMyFollows;
using CCE.Application.Community.Public.Queries.GetPublicPostById;
using CCE.Application.Community.Public.Queries.GetPublicTopicBySlug;
using CCE.Application.Community.Public.Queries.ListPublicPostReplies;
using CCE.Application.Community.Public.Queries.ListPublicPostsInTopic;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class CommunityPublicEndpoints
{
    public static IEndpointRouteBuilder MapCommunityPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var community = app.MapGroup("/api/community").WithTags("Community");

        community.MapGet("/topics/{slug}", async (
            string slug, IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetPublicTopicBySlugQuery(slug), ct).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        }).AllowAnonymous().WithName("GetPublicTopicBySlug");

        community.MapGet("/topics/{id:guid}/posts", async (
            System.Guid id, int? page, int? pageSize,
            IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ListPublicPostsInTopicQuery(id, page ?? 1, pageSize ?? 20), ct).ConfigureAwait(false);
            return Results.Ok(result);
        }).AllowAnonymous().WithName("ListPublicPostsInTopic");

        community.MapGet("/posts/{id:guid}", async (
            System.Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetPublicPostByIdQuery(id), ct).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        }).AllowAnonymous().WithName("GetPublicPostById");

        community.MapGet("/posts/{id:guid}/replies", async (
            System.Guid id, int? page, int? pageSize,
            IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ListPublicPostRepliesQuery(id, page ?? 1, pageSize ?? 20), ct).ConfigureAwait(false);
            return Results.Ok(result);
        }).AllowAnonymous().WithName("ListPublicPostReplies");

        var follows = app.MapGroup("/api/me/follows")
            .WithTags("Community")
            .RequireAuthorization();

        follows.MapGet("", async (
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();
            var dto = await mediator.Send(new GetMyFollowsQuery(userId), ct).ConfigureAwait(false);
            return Results.Ok(dto);
        }).WithName("GetMyFollows");

        return app;
    }
}
