using CCE.Api.Common.Extensions;
using CCE.Application.Content.Commands.ToggleNewsFollow;
using CCE.Application.Content.Public.Queries.GetPublicNewsById;
using CCE.Application.Content.Public.Queries.ListPublicNews;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class NewsPublicEndpoints
{
    public static IEndpointRouteBuilder MapNewsPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var news = app.MapGroup("/api/news").WithTags("News");

        news.MapGet("", async (
            int? page, int? pageSize, bool? isFeatured, System.Guid? topicId,
            [FromQuery] System.Guid[]? tagIds,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListPublicNewsQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                IsFeatured: isFeatured,
                TopicId: topicId,
                TagIds: tagIds);
            var response = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("ListPublicNews");

        news.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new GetPublicNewsByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetPublicNewsById");

        news.MapPatch("/follow", async (
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new ToggleNewsFollowCommand(), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization()
        .WithName("ToggleNewsFollow");

        return app;
    }
}
