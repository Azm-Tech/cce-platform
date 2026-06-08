using CCE.Api.Common.Extensions;
using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Commands.CastPollVote;
using CCE.Application.Community.Commands.CreatePost;
using CCE.Application.Community.Commands.CreateReply;
using CCE.Application.Community.Commands.DeleteDraft;
using CCE.Application.Community.Commands.FollowCommunity;
using CCE.Application.Community.Commands.JoinCommunity;
using CCE.Application.Community.Commands.LeaveCommunity;
using CCE.Application.Community.Commands.UnfollowCommunity;
using CCE.Application.Community.Commands.EditReply;
using CCE.Application.Community.Commands.FollowPost;
using CCE.Application.Community.Commands.FollowTopic;
using CCE.Application.Community.Commands.FollowUser;
using CCE.Application.Community.Commands.MarkPostAnswered;
using CCE.Application.Community.Commands.PublishPost;
using CCE.Application.Community.Commands.UnfollowPost;
using CCE.Application.Community.Commands.UnfollowTopic;
using CCE.Application.Community.Commands.UnfollowUser;
using CCE.Application.Community.Commands.UpdateDraft;
using CCE.Application.Community.Commands.VotePost;
using CCE.Application.Community.Commands.VoteReply;
using CCE.Domain;
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

        // POST /api/community/posts — create (publish or save as draft); logic-free (§A.4)
        community.MapPost("/posts", async (
            CreatePostRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var cmd = new CreatePostCommand(
                body.CommunityId, body.TopicId, body.Type, body.Title, body.Content, body.Locale,
                body.TagIds ?? System.Array.Empty<System.Guid>(),
                body.Attachments ?? System.Array.Empty<PostAttachmentInput>(),
                body.Poll,
                body.SaveAsDraft);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToCreatedHttpResult();
        }).RequireAuthorization(Permissions.Community_Post_Create).WithName("CreatePost");

        // PUT /api/community/posts/{id}/draft — edit a draft
        community.MapPut("/posts/{id:guid}/draft", async (
            System.Guid id, UpdateDraftRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new UpdateDraftCommand(
                id, body.Title, body.Content, body.TagIds ?? System.Array.Empty<System.Guid>());
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Community_Post_Create).WithName("UpdateDraft");

        // POST /api/community/posts/{id}/publish — publish a draft
        community.MapPost("/posts/{id:guid}/publish", async (
            System.Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new PublishPostCommand(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Community_Post_Create).WithName("PublishPost");

        // DELETE /api/community/posts/{id}/draft — discard an unpublished draft
        community.MapDelete("/posts/{id:guid}/draft", async (
            System.Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteDraftCommand(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Community_Post_Create).WithName("DeleteDraft");

        // POST /api/community/posts/{id}/replies — logic-free (§A.4); supports nesting + mentions
        community.MapPost("/posts/{id:guid}/replies", async (
            System.Guid id,
            CreateReplyRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var cmd = new CreateReplyCommand(id, body.Content, body.Locale, body.ParentReplyId,
                body.MentionedUserIds ?? System.Array.Empty<System.Guid>());
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToCreatedHttpResult();
        }).RequireAuthorization(Permissions.Community_Post_Reply).WithName("CreateReply");

        // POST /api/community/posts/{id}/vote — US027 up/down vote (logic-free; §A.4)
        community.MapPost("/posts/{id:guid}/vote", async (
            System.Guid id,
            VotePostRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new VotePostCommand(id, body.Direction), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Community_Post_Vote).WithName("VotePost");

        // POST /api/community/replies/{id}/vote — US027 up/down vote on a reply
        community.MapPost("/replies/{id:guid}/vote", async (
            System.Guid id,
            VoteReplyRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new VoteReplyCommand(id, body.Direction), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Community_Post_Vote).WithName("VoteReply");

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

        // POST /api/community/polls/{id}/vote — cast a poll vote
        community.MapPost("/polls/{id:guid}/vote", async (
            System.Guid id, CastPollVoteRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new CastPollVoteCommand(id, body.OptionIds ?? System.Array.Empty<System.Guid>());
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Community_Poll_Vote).WithName("CastPollVote");

        // Community membership & follow (logic-free; §A.4)
        community.MapPost("/communities/{id:guid}/join", async (
            System.Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new JoinCommunityCommand(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Community_Community_Join).WithName("JoinCommunity");

        community.MapPost("/communities/{id:guid}/leave", async (
            System.Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new LeaveCommunityCommand(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Community_Community_Join).WithName("LeaveCommunity");

        community.MapPost("/communities/{id:guid}/follow", async (
            System.Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new FollowCommunityCommand(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Community_Community_Join).WithName("FollowCommunity");

        community.MapDelete("/communities/{id:guid}/follow", async (
            System.Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UnfollowCommunityCommand(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Community_Community_Join).WithName("UnfollowCommunity");

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

public sealed record MarkAnswerRequest(Guid ReplyId);
public sealed record EditReplyRequest(string Content);
