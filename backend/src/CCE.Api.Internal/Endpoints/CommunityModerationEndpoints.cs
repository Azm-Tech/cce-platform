using CCE.Application.Community.Commands.SoftDeletePost;
using CCE.Application.Community.Commands.SoftDeleteReply;
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
