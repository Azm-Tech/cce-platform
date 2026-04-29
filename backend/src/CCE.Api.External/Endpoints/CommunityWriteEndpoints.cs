using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Commands.CreatePost;
using CCE.Application.Community.Commands.CreateReply;
using CCE.Application.Community.Commands.EditReply;
using CCE.Application.Community.Commands.FollowPost;
using CCE.Application.Community.Commands.FollowTopic;
using CCE.Application.Community.Commands.FollowUser;
using CCE.Application.Community.Commands.MarkPostAnswered;
using CCE.Application.Community.Commands.RatePost;
using CCE.Application.Community.Commands.UnfollowPost;
using CCE.Application.Community.Commands.UnfollowTopic;
using CCE.Application.Community.Commands.UnfollowUser;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class CommunityWriteEndpoints
{
    public static IEndpointRouteBuilder MapCommunityWriteEndpoints(this IEndpointRouteBuilder app)
    {
        var community = app.MapGroup("/api/community")
            .WithTags("Community")
            .RequireAuthorization();

        // POST /api/community/posts
        community.MapPost("/posts", async (
            CreatePostRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();

            var cmd = new CreatePostCommand(body.TopicId, body.Content, body.Locale, body.IsAnswerable);
            var id = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return Results.Created($"/api/community/posts/{id}", new { id });
        }).WithName("CreatePost");

        // POST /api/community/posts/{id}/replies
        community.MapPost("/posts/{id:guid}/replies", async (
            System.Guid id,
            CreateReplyRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();

            var cmd = new CreateReplyCommand(id, body.Content, body.Locale, body.ParentReplyId);
            var replyId = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return Results.Created($"/api/community/posts/{id}/replies/{replyId}", new { id = replyId });
        }).WithName("CreateReply");

        // POST /api/community/posts/{id}/rate
        community.MapPost("/posts/{id:guid}/rate", async (
            System.Guid id,
            RatePostRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();

            var cmd = new RatePostCommand(id, body.Stars);
            await mediator.Send(cmd, ct).ConfigureAwait(false);
            return Results.Ok();
        }).WithName("RatePost");

        // POST /api/community/posts/{id}/mark-answer
        community.MapPost("/posts/{id:guid}/mark-answer", async (
            System.Guid id,
            MarkAnswerRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();

            var cmd = new MarkPostAnsweredCommand(id, body.ReplyId);
            await mediator.Send(cmd, ct).ConfigureAwait(false);
            return Results.Ok();
        }).WithName("MarkPostAnswered");

        // PUT /api/community/replies/{id}
        community.MapPut("/replies/{id:guid}", async (
            System.Guid id,
            EditReplyRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();

            var cmd = new EditReplyCommand(id, body.Content);
            await mediator.Send(cmd, ct).ConfigureAwait(false);
            return Results.Ok();
        }).WithName("EditReply");

        // Follows group
        var follows = app.MapGroup("/api/me/follows")
            .WithTags("Community")
            .RequireAuthorization();

        // POST /api/me/follows/topics/{topicId}
        follows.MapPost("/topics/{topicId:guid}", async (
            System.Guid topicId,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();

            await mediator.Send(new FollowTopicCommand(topicId), ct).ConfigureAwait(false);
            return Results.Ok();
        }).WithName("FollowTopic");

        // DELETE /api/me/follows/topics/{topicId}
        follows.MapDelete("/topics/{topicId:guid}", async (
            System.Guid topicId,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();

            await mediator.Send(new UnfollowTopicCommand(topicId), ct).ConfigureAwait(false);
            return Results.NoContent();
        }).WithName("UnfollowTopic");

        // POST /api/me/follows/users/{userId}
        follows.MapPost("/users/{userId:guid}", async (
            System.Guid userId,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var actorId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (actorId == System.Guid.Empty) return Results.Unauthorized();

            await mediator.Send(new FollowUserCommand(userId), ct).ConfigureAwait(false);
            return Results.Ok();
        }).WithName("FollowUser");

        // DELETE /api/me/follows/users/{userId}
        follows.MapDelete("/users/{userId:guid}", async (
            System.Guid userId,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var actorId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (actorId == System.Guid.Empty) return Results.Unauthorized();

            await mediator.Send(new UnfollowUserCommand(userId), ct).ConfigureAwait(false);
            return Results.NoContent();
        }).WithName("UnfollowUser");

        // POST /api/me/follows/posts/{postId}
        follows.MapPost("/posts/{postId:guid}", async (
            System.Guid postId,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();

            await mediator.Send(new FollowPostCommand(postId), ct).ConfigureAwait(false);
            return Results.Ok();
        }).WithName("FollowPost");

        // DELETE /api/me/follows/posts/{postId}
        follows.MapDelete("/posts/{postId:guid}", async (
            System.Guid postId,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();

            await mediator.Send(new UnfollowPostCommand(postId), ct).ConfigureAwait(false);
            return Results.NoContent();
        }).WithName("UnfollowPost");

        return app;
    }
}

public sealed record CreatePostRequest(Guid TopicId, string Content, string Locale, bool IsAnswerable);
public sealed record CreateReplyRequest(string Content, string Locale, Guid? ParentReplyId);
public sealed record RatePostRequest(int Stars);
public sealed record MarkAnswerRequest(Guid ReplyId);
public sealed record EditReplyRequest(string Content);
