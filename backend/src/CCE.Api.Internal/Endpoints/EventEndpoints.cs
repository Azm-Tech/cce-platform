using CCE.Application.Content.Commands.CreateEvent;
using CCE.Application.Content.Queries.GetEventById;
using CCE.Application.Content.Queries.ListEvents;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var events = app.MapGroup("/api/admin/events").WithTags("Events");

        events.MapGet("", async (
            int? page, int? pageSize, string? search,
            System.DateTimeOffset? fromDate, System.DateTimeOffset? toDate,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListEventsQuery(page ?? 1, pageSize ?? 20, search, fromDate, toDate);
            var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.Event_Manage)
        .WithName("ListEvents");

        events.MapGet("/{id:guid}", async (System.Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var dto = await mediator.Send(new GetEventByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Event_Manage)
        .WithName("GetEventById");

        events.MapPost("", async (CreateEventRequest body, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new CreateEventCommand(
                body.TitleAr, body.TitleEn, body.DescriptionAr, body.DescriptionEn,
                body.StartsOn, body.EndsOn,
                body.LocationAr, body.LocationEn,
                body.OnlineMeetingUrl, body.FeaturedImageUrl);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/admin/events/{dto.Id}", dto);
        })
        .RequireAuthorization(Permissions.Event_Manage)
        .WithName("CreateEvent");

        return app;
    }
}

public sealed record CreateEventRequest(
    string TitleAr, string TitleEn,
    string DescriptionAr, string DescriptionEn,
    System.DateTimeOffset StartsOn,
    System.DateTimeOffset EndsOn,
    string? LocationAr,
    string? LocationEn,
    string? OnlineMeetingUrl,
    string? FeaturedImageUrl);
