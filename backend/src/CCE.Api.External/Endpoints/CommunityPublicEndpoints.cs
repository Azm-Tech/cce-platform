using CCE.Api.Common.Extensions;
using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Public.Queries.GetCommunityBySlug;
using CCE.Application.Community.Public.Queries.GetCommunityUserProfile;
using CCE.Application.Community.Public.Queries.GetMyFollows;
using CCE.Application.Community.Public.Queries.GetPostShareLink;
using CCE.Application.Community.Public.Queries.GetPublicPostById;
using CCE.Application.Community.Public.Queries.GetPollResults;
using CCE.Application.Community.Public.Queries.GetPublicTopicBySlug;
using CCE.Application.Community.Public.Queries.GetReplyThread;
using CCE.Application.Community.Public.Queries.ListMyDrafts;
using CCE.Application.Community.Public.Queries.ListMyMentions;
using CCE.Application.Community.Public.Queries.ListPublicCommunities;
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

        // GET /api/community/communities — list public communities
        community.MapGet("/communities", async (
            int? page, int? pageSize, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ListPublicCommunitiesQuery(page ?? 1, pageSize ?? 20), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).AllowAnonymous().WithName("ListPublicCommunities");

        // GET /api/community/communities/{slug} — community by slug
        community.MapGet("/communities/{slug}", async (
            string slug, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCommunityBySlugQuery(slug), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).AllowAnonymous().WithName("GetCommunityBySlug");

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

        // GET /api/community/polls/{id}/results — poll tallies (hidden until close when configured)
        community.MapGet("/polls/{id:guid}/results", async (
            System.Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPollResultsQuery(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).AllowAnonymous().WithName("GetPollResults");

        // GET /api/community/users/{id} — US030 community user profile
        community.MapGet("/users/{id:guid}", async (
            System.Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCommunityUserProfileQuery(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization().WithName("GetCommunityUserProfile");

        // GET /api/community/posts/{id}/share — US025 shareable link
        community.MapGet("/posts/{id:guid}/share", async (
            System.Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPostShareLinkQuery(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).AllowAnonymous().WithName("GetPostShareLink");

        // GET /api/community/replies/{id}/thread — descendant subtree of a reply
        community.MapGet("/replies/{id:guid}/thread", async (
            System.Guid id, int? page, int? pageSize, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetReplyThreadQuery(id, page ?? 1, pageSize ?? 20), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).AllowAnonymous().WithName("GetReplyThread");

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

        // GET /api/me/posts/drafts — the caller's own unpublished drafts
        var me = app.MapGroup("/api/me/posts").WithTags("Community").RequireAuthorization();
        me.MapGet("/drafts", async (int? page, int? pageSize, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ListMyDraftsQuery(page ?? 1, pageSize ?? 20), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).WithName("ListMyDrafts");

        // GET /api/me/mentions — where the caller was @mentioned
        var mentions = app.MapGroup("/api/me/mentions").WithTags("Community").RequireAuthorization();
        mentions.MapGet("", async (int? page, int? pageSize, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ListMyMentionsQuery(page ?? 1, pageSize ?? 20), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).WithName("ListMyMentions");

        return app;
    }
}
