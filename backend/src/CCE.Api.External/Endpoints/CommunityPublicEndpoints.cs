using CCE.Api.Common.Extensions;
using CCE.Api.Common.Results;
using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Public.Queries.GetCommunityBySlug;
using CCE.Application.Community.Public.Queries.GetCommunityUserProfile;
using CCE.Application.Community.Public.Queries.GetMyFollows;
using CCE.Application.Community.Public.Queries.GetPostActivity;
using CCE.Application.Community.Public.Queries.GetPostShareLink;
using CCE.Application.Community.Public.Queries.GetPublicPostById;
using CCE.Application.Community.Public.Queries.GetPollResults;
using CCE.Application.Community.Public.Queries.GetPublicTopicBySlug;
using CCE.Application.Community.Public.Queries.GetCommunityRoles;
using CCE.Application.Community.Public.Queries.GetReplyThread;
using CCE.Application.Community.Public.Queries.ListCommunityFeed;
using CCE.Application.Community.Public.Queries.ListExpertLeaderboard;
using CCE.Application.Community.Public.Queries.ListMyDrafts;
using CCE.Application.Community.Public.Queries.GetMyTopics;
using CCE.Application.Community.Public.Queries.ListMyMentions;
using CCE.Application.Community.Public.Queries.GetMentionableUsers;
using CCE.Application.Community.Public.Queries.ListUserFeed;
using CCE.Application.Community.Public.Queries.SearchCommunityPosts;
using CCE.Application.Community.Public.Queries.ListPublicCommunities;
using CCE.Application.Community.Public.Queries.ListPublicPostReplies;
using CCE.Application.Community.Public.Queries.ListPublicPostsInTopic;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Community.Public.Queries.ListPublicTopicsPaginated;
using CCE.Api.Common.Observability;
using CCE.Domain;
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

        // GET /api/community/feed — community home feed with optional full-text search.
        // When ?searchTerm= is supplied, dispatches to SearchCommunityPostsQuery (Meilisearch) and returns
        // the same CommunityFeedItemDto shape, enriched with highlight fields.
        // When ?searchTerm= is absent, behaves exactly as before (Redis fan-out / SQL path).
        community.MapGet("/feed", async (
            string? searchTerm,
            PostFeedSort? sort, System.Guid[]? tagIds, System.Guid? communityId, System.Guid? topicId,
            CCE.Domain.Community.PostType? postType, int? page, int? pageSize,
            System.Guid? authorId, bool? isWatchlisted,
            ICurrentUserAccessor currentUser, IMediator mediator, CancellationToken ct) =>
        {
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var effectiveSort = sort?.ToString() ?? "relevance";
                var searchQuery = new SearchCommunityPostsQuery(
                    searchTerm,
                    sort,
                    tagIds ?? System.Array.Empty<System.Guid>(),
                    communityId,
                    topicId,
                    currentUser.GetUserId(),
                    postType,
                    page ?? 1,
                    pageSize ?? 20,
                    AuthorId: authorId);
                var searchResult = await mediator.Send(searchQuery, ct).ConfigureAwait(false);
                sw.Stop();
                PrometheusExtensions.CommunitySearchDurationMs.Observe(sw.Elapsed.TotalMilliseconds);
                PrometheusExtensions.CommunitySearchHitsTotal.WithLabels(effectiveSort).Inc();
                return searchResult.ToHttpResult();
            }

            var query = new ListCommunityFeedQuery(
                sort ?? PostFeedSort.Hot,
                tagIds ?? System.Array.Empty<System.Guid>(),
                communityId,
                topicId,
                currentUser.GetUserId(),
                postType,
                page ?? 1,
                pageSize ?? 20,
                AuthorId: authorId,
                IsWatchlisted: isWatchlisted);
            var result = await mediator.Send(query, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).AllowAnonymous().WithName("ListCommunityFeed");

        // GET /api/community/experts/leaderboard — top experts by contribution count
        community.MapGet("/experts/leaderboard", async (
            int? page, int? pageSize, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ListExpertLeaderboardQuery(page ?? 1, pageSize ?? 20), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).AllowAnonymous().WithName("ListExpertLeaderboard");

        // GET /api/community/roles — fixed community membership role definitions
        community.MapGet("/roles", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCommunityRolesQuery(), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).AllowAnonymous().WithName("GetCommunityRoles");

        // GET /api/community/communities/{slug} — community by slug
        community.MapGet("/communities/{slug}", async (
            string slug, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCommunityBySlugQuery(slug), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).AllowAnonymous().WithName("GetCommunityBySlug");

        // GET /api/community/communities/{communityId}/mentionable-users?q=rash — @mention autocomplete (2-tier)
        community.MapGet("/communities/{communityId:guid}/mentionable-users", async (
            System.Guid communityId, string? q, int? limit,
            IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetMentionableUsersQuery(communityId, q ?? string.Empty, limit ?? 10), ct)
                .ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Community_Post_Reply).WithName("GetMentionableUsers");

        // GET /api/community/topics — global topics discovery (paginated, searchable, sortable)
        community.MapGet("/topics", async (
            string? search, TopicsSortBy? sortBy, int? page, int? pageSize,
            IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ListPublicTopicsPaginatedQuery(search, sortBy, page ?? 1, pageSize ?? 20), ct)
                .ConfigureAwait(false);
            return result.ToHttpResult();
        }).AllowAnonymous().WithName("ListPublicTopicsPaginated");

        community.MapGet("/topics/{slug}", async (
            string slug, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPublicTopicBySlugQuery(slug), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).AllowAnonymous().WithName("GetPublicTopicBySlug");

        community.MapGet("/topics/{id:guid}/posts", async (
            System.Guid id, int? page, int? pageSize,
            ICurrentUserAccessor currentUser, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ListPublicPostsInTopicQuery(id, currentUser.GetUserId(), page ?? 1, pageSize ?? 20), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).AllowAnonymous().WithName("ListPublicPostsInTopic");

        community.MapGet("/posts/{id:guid}", async (
            System.Guid id, ICurrentUserAccessor currentUser, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPublicPostByIdQuery(id, currentUser.GetUserId()), ct)
                .ConfigureAwait(false);
            return result.ToHttpResult();
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
            System.Guid id, ICurrentUserAccessor currentUser, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCommunityUserProfileQuery(id, currentUser.GetUserId()), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).AllowAnonymous().WithName("GetCommunityUserProfile");

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
            return result.ToHttpResult();
        }).AllowAnonymous().WithName("ListPublicPostReplies");

        // GET /api/community/posts/{id}/activity?since={ISO8601} — Phase 3 reconnect catch-up.
        // Returns current vote counters, replies created since the cursor, and a poll snapshot.
        // Called by mobile on onreconnected after a SignalR drop.
        community.MapGet("/posts/{id:guid}/activity", async (
            System.Guid id, System.DateTimeOffset since,
            ICurrentUserAccessor currentUser, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetPostActivityQuery(id, since, currentUser.GetUserId()), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).AllowAnonymous().WithName("GetPostActivity");

        var follows = app.MapGroup("/api/me/follows")
            .WithTags("Community")
            .RequireAuthorization();

        follows.MapGet("", async (
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return EnvelopeResults.Unauthorized();
            var result = await mediator.Send(new GetMyFollowsQuery(userId), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).WithName("GetMyFollows");

        // GET /api/me/posts — the caller's own published posts (same filters/sorting as community feed)
        // GET /api/me/feed — personal home feed with the same filters as community feed
        var myFeed = app.MapGroup("/api/me").WithTags("Community").RequireAuthorization();
        myFeed.MapGet("/feed", async (
            PostFeedSort? sort, System.Guid[]? tagIds, System.Guid? communityId, System.Guid? topicId,
            CCE.Domain.Community.PostType? postType, int? page, int? pageSize,
            ICurrentUserAccessor currentUser, IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId();
            if (!userId.HasValue) return EnvelopeResults.Unauthorized();
            var result = await mediator.Send(
                new ListUserFeedQuery(
                    userId.Value,
                    sort ?? PostFeedSort.Newest,
                    tagIds ?? System.Array.Empty<System.Guid>(),
                    communityId,
                    topicId,
                    postType,
                    page ?? 1,
                    pageSize ?? 20), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).WithName("ListUserFeed");

        // GET /api/me/posts/drafts — the caller's own unpublished drafts
        var me = app.MapGroup("/api/me/posts").WithTags("Community").RequireAuthorization();
        me.MapGet("", async (
            PostFeedSort? sort, System.Guid[]? tagIds, System.Guid? communityId, System.Guid? topicId,
            CCE.Domain.Community.PostType? postType, int? page, int? pageSize,
            ICurrentUserAccessor currentUser, IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId();
            if (userId is null || userId == System.Guid.Empty)
                return EnvelopeResults.Unauthorized();
            var query = new ListCommunityFeedQuery(
                sort ?? PostFeedSort.Newest,
                tagIds ?? System.Array.Empty<System.Guid>(),
                communityId,
                topicId,
                userId,
                postType,
                page ?? 1,
                pageSize ?? 20,
                AuthorId: userId);
            var result = await mediator.Send(query, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).WithName("ListMyPosts");
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

        // GET /api/me/topics — topics followed by the caller
        var meTopics = app.MapGroup("/api/me/topics").WithTags("Community").RequireAuthorization();
        meTopics.MapGet("", async (
            string? search, int? page, int? pageSize,
            IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetMyTopicsQuery(search, page ?? 1, pageSize ?? 20), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).WithName("GetMyTopics");

        return app;
    }
}
