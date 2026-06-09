using CCE.Api.Common.Extensions;
using CCE.Application.Community.Commands.ApproveJoinRequest;
using CCE.Application.Community.Commands.ChangeCommunityVisibility;
using CCE.Application.Community.Commands.CreateCommunity;
using CCE.Application.Community.Commands.RejectJoinRequest;
using CCE.Application.Community.Commands.SoftDeletePost;
using CCE.Application.Community.Commands.SoftDeleteReply;
using CCE.Application.Community.Commands.UpdateCommunity;
using CCE.Application.Community.Queries.ListAdminPosts;
using CCE.Application.Community.Queries.ListJoinRequests;
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

        // ─── Community management ───
        moderation.MapPost("/communities", async (
            CreateCommunityRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new CreateCommunityCommand(body.NameAr, body.NameEn, body.DescriptionAr,
                body.DescriptionEn, body.Slug, body.Visibility, body.PresentationJson);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToCreatedHttpResult();
        }).RequireAuthorization(Permissions.Community_Community_Create).WithName("CreateCommunity");

        moderation.MapPut("/communities/{id:guid}", async (
            System.Guid id, UpdateCommunityRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new UpdateCommunityCommand(id, body.NameAr, body.NameEn,
                body.DescriptionAr, body.DescriptionEn, body.PresentationJson);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Community_Community_Update).WithName("UpdateCommunity");

        moderation.MapPost("/communities/{id:guid}/visibility", async (
            System.Guid id, ChangeCommunityVisibilityRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ChangeCommunityVisibilityCommand(id, body.Visibility), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Community_Community_Update).WithName("ChangeCommunityVisibility");

        moderation.MapGet("/communities/{id:guid}/join-requests", async (
            System.Guid id, int? page, int? pageSize, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ListJoinRequestsQuery(id, page ?? 1, pageSize ?? 20), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Community_Community_Moderate).WithName("ListJoinRequests");

        moderation.MapPost("/join-requests/{id:guid}/approve", async (
            System.Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ApproveJoinRequestCommand(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Community_Community_Moderate).WithName("ApproveJoinRequest");

        moderation.MapPost("/join-requests/{id:guid}/reject", async (
            System.Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RejectJoinRequestCommand(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).RequireAuthorization(Permissions.Community_Community_Moderate).WithName("RejectJoinRequest");

        return app;
    }
}
