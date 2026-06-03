using CCE.Api.Common.Extensions;
using CCE.Application.Content.Commands.Tags.CreateTag;
using CCE.Application.Content.Commands.Tags.DeleteTag;
using CCE.Application.Content.Commands.Tags.UpdateTag;
using CCE.Application.Content.Queries.Tags.GetTagById;
using CCE.Application.Content.Queries.Tags.ListTags;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class TagEndpoints
{
    public static IEndpointRouteBuilder MapTagEndpoints(this IEndpointRouteBuilder app)
    {
        var tags = app.MapGroup("/api/admin/tags").WithTags("Tags");

        tags.MapGet("", async (
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new ListTagsQuery(), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .WithName("ListTags");

        tags.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new GetTagByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .WithName("GetTagById");

        tags.MapPost("", async (
            CreateTagRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new CreateTagCommand(body.NameAr, body.NameEn, body.Color);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .WithName("CreateTag");

        tags.MapPut("/{id:guid}", async (
            System.Guid id,
            UpdateTagRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new UpdateTagCommand(id, body.NameAr, body.NameEn, body.Color);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .WithName("UpdateTag");

        tags.MapDelete("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new DeleteTagCommand(id), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .WithName("DeleteTag");

        return app;
    }
}

public sealed record CreateTagRequest(string NameAr, string NameEn, string? Color);
public sealed record UpdateTagRequest(string NameAr, string NameEn, string? Color);
