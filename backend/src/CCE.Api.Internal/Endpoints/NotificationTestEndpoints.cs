using CCE.Api.Common.Extensions;
using CCE.Application.Notifications.Admin.Commands.SendTestPush;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class NotificationTestEndpoints
{
    public static IEndpointRouteBuilder MapNotificationTestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/notifications/test-push", async (
            SendTestPushRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new SendTestPushCommand(body.Token, body.Title, body.Body), ct)
                .ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithTags("Notifications")
        .WithName("SendTestPush")
        .RequireAuthorization(Permissions.Notification_Send);

        return app;
    }
}

public sealed record SendTestPushRequest(string Token, string Title, string Body);
