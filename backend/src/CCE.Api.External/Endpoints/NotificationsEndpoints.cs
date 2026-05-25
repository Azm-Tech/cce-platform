using CCE.Api.Common.Extensions;
using CCE.Application.Common.Interfaces;
using CCE.Application.Notifications.Public.Commands.MarkAllNotificationsRead;
using CCE.Application.Notifications.Public.Commands.MarkNotificationRead;
using CCE.Application.Notifications.Public.Commands.UpdateMyNotificationSettings;
using CCE.Application.Notifications.Public.Queries.GetMyNotificationSettings;
using CCE.Application.Notifications.Public.Queries.GetMyUnreadCount;
using CCE.Application.Notifications.Public.Queries.ListMyNotifications;
using CCE.Domain.Notifications;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var notif = app.MapGroup("/api/me/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        notif.MapGet("", async (
            int? page, int? pageSize, NotificationStatus? status,
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();
            var query = new ListMyNotificationsQuery(userId, page ?? 1, pageSize ?? 20, status);
            var result = await mediator.Send(query, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).WithName("ListMyNotifications");

        notif.MapGet("/unread-count", async (
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();
            var result = await mediator.Send(new GetMyUnreadCountQuery(userId), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).WithName("GetMyUnreadNotificationCount");

        notif.MapPost("/{id:guid}/mark-read", async (
            System.Guid id,
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();
            var result = await mediator.Send(new MarkNotificationReadCommand(id, userId), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).WithName("MarkNotificationRead");

        notif.MapPost("/mark-all-read", async (
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();
            var result = await mediator.Send(new MarkAllNotificationsReadCommand(userId), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).WithName("MarkAllNotificationsRead");

        notif.MapGet("/settings", async (
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();
            var result = await mediator.Send(new GetMyNotificationSettingsQuery(userId), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).WithName("GetMyNotificationSettings");

        notif.MapPut("/settings", async (
            UpdateMyNotificationSettingsRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();
            var command = new UpdateMyNotificationSettingsCommand(
                userId,
                body.Channel,
                body.IsEnabled,
                body.EventCode);
            var result = await mediator.Send(command, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).WithName("UpdateMyNotificationSettings");

        return app;
    }
}
