using CCE.Api.Common.Extensions;
using CCE.Application.Content.Commands.CreateEvent;
using CCE.Application.Content.Commands.DeleteEvent;
using CCE.Application.Content.Commands.RescheduleEvent;
using CCE.Application.Content.Commands.UpdateEvent;
using CCE.Application.Content.Queries.GetEventById;
using CCE.Application.Content.Queries.ListEvents;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var events = app.MapGroup("/api/admin/events").WithTags("Events");

        events.MapGet("", async (
            int? page, int? pageSize, string? search,
            System.DateTimeOffset? fromDate, System.DateTimeOffset? toDate, System.Guid? topicId,
            [FromQuery] System.Guid[]? tagIds,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListEventsQuery(page ?? 1, pageSize ?? 20, search, fromDate, toDate, topicId, tagIds);
            var response = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Event_Manage)
        .WithName("ListEvents");

        events.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new GetEventByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Event_Manage)
        .WithName("GetEventById");

        events.MapPost("", async (
            CreateEventRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new CreateEventCommand(
                body.TitleAr, body.TitleEn, body.DescriptionAr, body.DescriptionEn,
                body.StartsOn, body.EndsOn,
                body.LocationAr, body.LocationEn,
                body.OnlineMeetingUrl, body.FeaturedImageUrl,
                body.TopicId, body.TagIds);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Event_Manage)
        .WithName("CreateEvent");

        events.MapPut("/{id:guid}", async (
            System.Guid id,
            UpdateEventRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new UpdateEventCommand(
                id,
                body.TitleAr, body.TitleEn,
                body.DescriptionAr, body.DescriptionEn,
                body.LocationAr, body.LocationEn,
                body.OnlineMeetingUrl, body.FeaturedImageUrl,
                body.TopicId, body.TagIds);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Event_Manage)
        .WithName("UpdateEvent");

        events.MapPost("/{id:guid}/reschedule", async (
            System.Guid id,
            RescheduleEventRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new RescheduleEventCommand(id, body.StartsOn, body.EndsOn);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Event_Manage)
        .WithName("RescheduleEvent");

        events.MapDelete("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new DeleteEventCommand(id), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Event_Manage)
        .WithName("DeleteEvent");

        return app;
    }
}
