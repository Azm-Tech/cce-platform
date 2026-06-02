using CCE.Api.Common.Extensions;
using CCE.Application.Common;
using CCE.Application.Content.Public;
using CCE.Application.Content.Public.Queries.GetPublicEventById;
using CCE.Application.Content.Public.Queries.ListPublicEvents;
using CCE.Domain.Content;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class EventsPublicEndpoints
{
    public static IEndpointRouteBuilder MapEventsPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var events = app.MapGroup("/api/events").WithTags("Events");

        events.MapGet("", async (
            int? page, int? pageSize,
            System.DateTimeOffset? from, System.DateTimeOffset? to,
            string? topicSlug,
            EventSortBy? sortBy, SortOrder? sortOrder,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListPublicEventsQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                From: from,
                To: to,
                TopicSlug: topicSlug,
                SortBy: sortBy ?? EventSortBy.Date,
                SortOrder: sortOrder ?? SortOrder.Descending);
            var response = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("ListPublicEvents");

        events.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new GetPublicEventByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetPublicEventById");

        events.MapGet("/{id:guid}.ics", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new GetPublicEventByIdQuery(id), cancellationToken).ConfigureAwait(false);
            if (!response.Success)
                return response.ToHttpResult();
            var ics = IcsBuilder.ToIcs(response.Data!);
            return Results.Text(ics, "text/calendar; charset=utf-8");
        })
        .AllowAnonymous()
        .WithName("GetPublicEventIcs");

        return app;
    }
}
