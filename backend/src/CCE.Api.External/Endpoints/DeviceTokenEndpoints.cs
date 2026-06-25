using CCE.Api.Common.Extensions;
using CCE.Api.Common.Results;
using CCE.Application.Common.Interfaces;
using CCE.Application.Notifications.Public.Commands.RegisterDeviceToken;
using CCE.Application.Notifications.Public.Commands.UnregisterDeviceToken;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class DeviceTokenEndpoints
{
    public static IEndpointRouteBuilder MapDeviceTokenEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/me/device-tokens")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapPost("", async (
            RegisterDeviceTokenRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return EnvelopeResults.Unauthorized();
            var cmd = new RegisterDeviceTokenCommand(userId, body.Token, body.Platform, body.DeviceId);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("RegisterDeviceToken")
        .RequireAuthorization(Permissions.Notification_DeviceToken_Register);

        group.MapDelete("/{deviceId}", async (
            string deviceId,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return EnvelopeResults.Unauthorized();
            var cmd = new UnregisterDeviceTokenCommand(userId, deviceId);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("UnregisterDeviceToken")
        .RequireAuthorization(Permissions.Notification_DeviceToken_Delete);

        return app;
    }
}

public sealed record RegisterDeviceTokenRequest(
    string Token,
    string Platform,
    string DeviceId);
