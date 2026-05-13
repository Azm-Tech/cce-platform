using CCE.Application.Community.Commands.SoftDeletePost;
using CCE.Application.Community.Commands.SoftDeleteReply;
using CCE.Application.Community.Queries.ListAdminPosts;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class CommunityModerationEndpoints
{
    public static IEndpointRouteBuilder MapCommunityModerationEndpoints(this IEndpointRouteBuilder app)
    {
        var moderation = app.MapGroup("/api/admin/community").WithTags("CommunityModeration");

        // GET /api/admin/community/posts — paginated moderation list.
        // Supports query params: ?page=&pageSize=&topicId=&search=&status=&locale=
        // status ∈ { "all", "active", "deleted", "question", "answered" }
        moderation.MapGet("/posts", async (
            int? page,
            int? pageSize,
            System.Guid? topicId,
            string? search,
            string? status,
            string? locale,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new ListAdminPostsQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                TopicId: topicId,
                Search: search,
                Status: status,
                Locale: locale), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.Community_Post_Moderate)
        .WithName("ListAdminPosts");

        moderation.MapDelete("/posts/{id:guid}", async (
            System.Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new SoftDeletePostCommand(id), cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        })
        .RequireAuthorization(Permissions.Community_Post_Moderate)
        .WithName("SoftDeletePost");

        moderation.MapDelete("/replies/{id:guid}", async (
            System.Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new SoftDeleteReplyCommand(id), cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        })
        .RequireAuthorization(Permissions.Community_Post_Moderate)
        .WithName("SoftDeleteReply");

        return app;
    }
}
