using CCE.Application.Community.Commands.CreateTopic;
using CCE.Application.Community.Commands.DeleteTopic;
using CCE.Application.Community.Commands.UpdateTopic;
using CCE.Application.Community.Queries.GetTopicById;
using CCE.Application.Community.Queries.ListTopics;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class TopicEndpoints
{
    public static IEndpointRouteBuilder MapTopicEndpoints(this IEndpointRouteBuilder app)
    {
        var topics = app.MapGroup("/api/admin/topics").WithTags("Topics");

        topics.MapGet("", async (
            int? page, int? pageSize, System.Guid? parentId, bool? isActive, string? search,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListTopicsQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                ParentId: parentId,
                IsActive: isActive,
                Search: search);
            var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.Community_Post_Moderate)
        .WithName("ListTopics");

        topics.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var dto = await mediator.Send(new GetTopicByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Community_Post_Moderate)
        .WithName("GetTopicById");

        topics.MapPost("", async (
            CreateTopicRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new CreateTopicCommand(
                body.NameAr, body.NameEn,
                body.DescriptionAr, body.DescriptionEn,
                body.Slug, body.ParentId, body.IconUrl, body.OrderIndex);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/admin/topics/{dto.Id}", dto);
        })
        .RequireAuthorization(Permissions.Community_Post_Moderate)
        .WithName("CreateTopic");

        topics.MapPut("/{id:guid}", async (
            System.Guid id,
            UpdateTopicRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new UpdateTopicCommand(
                id, body.NameAr, body.NameEn,
                body.DescriptionAr, body.DescriptionEn,
                body.OrderIndex, body.IsActive);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Community_Post_Moderate)
        .WithName("UpdateTopic");

        topics.MapDelete("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new DeleteTopicCommand(id), cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        })
        .RequireAuthorization(Permissions.Community_Post_Moderate)
        .WithName("DeleteTopic");

        return app;
    }
}

public sealed record CreateTopicRequest(
    string NameAr,
    string NameEn,
    string DescriptionAr,
    string DescriptionEn,
    string Slug,
    System.Guid? ParentId,
    string? IconUrl,
    int OrderIndex);

public sealed record UpdateTopicRequest(
    string NameAr,
    string NameEn,
    string DescriptionAr,
    string DescriptionEn,
    int OrderIndex,
    bool IsActive);
