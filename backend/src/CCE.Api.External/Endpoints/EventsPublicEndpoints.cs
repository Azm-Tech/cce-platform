using CCE.Application.Content.Public;
using CCE.Application.Content.Public.Queries.GetPublicEventById;
using CCE.Application.Content.Public.Queries.ListPublicEvents;
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
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListPublicEventsQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                From: from,
                To: to);
            var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("ListPublicEvents");

        events.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var dto = await mediator.Send(new GetPublicEventByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .AllowAnonymous()
        .WithName("GetPublicEventById");

        events.MapGet("/{id:guid}.ics", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var dto = await mediator.Send(new GetPublicEventByIdQuery(id), cancellationToken).ConfigureAwait(false);
            if (dto is null)
                return Results.NotFound();
            var ics = IcsBuilder.ToIcs(dto);
            return Results.Text(ics, "text/calendar; charset=utf-8");
        })
        .AllowAnonymous()
        .WithName("GetPublicEventIcs");

        return app;
    }
}
