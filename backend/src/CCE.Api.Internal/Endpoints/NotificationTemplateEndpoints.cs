using CCE.Application.Notifications.Commands.CreateNotificationTemplate;
using CCE.Application.Notifications.Commands.UpdateNotificationTemplate;
using CCE.Application.Notifications.Queries.GetNotificationTemplateById;
using CCE.Application.Notifications.Queries.ListNotificationTemplates;
using CCE.Domain;
using CCE.Domain.Notifications;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class NotificationTemplateEndpoints
{
    public static IEndpointRouteBuilder MapNotificationTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/notification-templates").WithTags("NotificationTemplates");

        group.MapGet("", async (
            int? page, int? pageSize,
            NotificationChannel? channel, bool? isActive,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListNotificationTemplatesQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                Channel: channel,
                IsActive: isActive);
            var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.Notification_TemplateManage)
        .WithName("ListNotificationTemplates");

        group.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var dto = await mediator.Send(new GetNotificationTemplateByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Notification_TemplateManage)
        .WithName("GetNotificationTemplateById");

        group.MapPost("", async (
            CreateNotificationTemplateRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new CreateNotificationTemplateCommand(
                body.Code,
                body.SubjectAr, body.SubjectEn,
                body.BodyAr, body.BodyEn,
                body.Channel,
                body.VariableSchemaJson);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/admin/notification-templates/{dto.Id}", dto);
        })
        .RequireAuthorization(Permissions.Notification_TemplateManage)
        .WithName("CreateNotificationTemplate");

        group.MapPut("/{id:guid}", async (
            System.Guid id,
            UpdateNotificationTemplateRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new UpdateNotificationTemplateCommand(
                id,
                body.SubjectAr, body.SubjectEn,
                body.BodyAr, body.BodyEn,
                body.IsActive);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Notification_TemplateManage)
        .WithName("UpdateNotificationTemplate");

        return app;
    }
}

public sealed record CreateNotificationTemplateRequest(
    string Code,
    string SubjectAr,
    string SubjectEn,
    string BodyAr,
    string BodyEn,
    CCE.Domain.Notifications.NotificationChannel Channel,
    string VariableSchemaJson);

public sealed record UpdateNotificationTemplateRequest(
    string SubjectAr,
    string SubjectEn,
    string BodyAr,
    string BodyEn,
    bool IsActive);
