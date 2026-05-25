using CCE.Api.Common.Extensions;
using CCE.Application.Notifications.Admin.Commands.RetryNotificationLog;
using CCE.Application.Notifications.Admin.Queries.GetNotificationLogById;
using CCE.Application.Notifications.Admin.Queries.ListNotificationLogs;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class NotificationLogEndpoints
{
    public static IEndpointRouteBuilder MapNotificationLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/notification-logs")
            .WithTags("Notification Logs")
            .RequireAuthorization(Permissions.Notification_LogView);

        group.MapGet("", async (
            int? page, int? pageSize,
            Guid? recipientUserId, string? templateCode, int? channel, int? status,
            IMediator mediator, CancellationToken ct) =>
        {
            var query = new ListNotificationLogsQuery(
                page ?? 1,
                pageSize ?? 20,
                recipientUserId,
                templateCode,
                channel is { } c ? (CCE.Domain.Notifications.NotificationChannel)c : null,
                status is { } s ? (CCE.Domain.Notifications.NotificationDeliveryStatus)s : null);
            var result = await mediator.Send(query, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).WithName("ListNotificationLogs");

        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetNotificationLogByIdQuery(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).WithName("GetNotificationLogById");

        group.MapPost("/{id:guid}/retry", async (
            Guid id,
            IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RetryNotificationLogCommand(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        }).WithName("RetryNotificationLog");

        return app;
    }
}
