using CCE.Api.Common.Extensions;
using CCE.Application.Common;
using CCE.Application.PlatformSettings.Commands.CreatePolicySection;
using CCE.Application.PlatformSettings.Commands.DeletePolicySection;
using CCE.Application.PlatformSettings.Commands.UpdatePoliciesSettings;
using CCE.Application.PlatformSettings.Commands.UpdatePolicySection;
using CCE.Application.PlatformSettings.Queries.GetPoliciesSettings;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class PoliciesSettingsEndpoints
{
    public static IEndpointRouteBuilder MapPoliciesSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var policies = app.MapGroup("/api/admin/settings/policies").WithTags("PlatformSettings");

        policies.MapGet("", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPoliciesSettingsQuery(), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Page_PolicyEdit)
        .WithName("GetPoliciesSettings");

        policies.MapPut("", async (UpdatePoliciesSettingsRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var rowVersion = string.IsNullOrEmpty(body.RowVersion)
                ? System.Array.Empty<byte>()
                : System.Convert.FromBase64String(body.RowVersion);
            var cmd = new UpdatePoliciesSettingsCommand(rowVersion);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Page_PolicyEdit)
        .WithName("UpdatePoliciesSettings");

        policies.MapPost("/sections", async (
            CreatePolicySectionRequest body,
            IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new CreatePolicySectionCommand(
                body.Type, body.TitleAr, body.TitleEn, body.ContentAr, body.ContentEn);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToCreatedHttpResult();
        })
        .RequireAuthorization(Permissions.Page_PolicyEdit)
        .WithName("CreatePolicySection");

        policies.MapPut("/sections/{id:guid}", async (
            System.Guid id,
            UpdatePolicySectionRequest body,
            IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new UpdatePolicySectionCommand(
                id, body.TitleAr, body.TitleEn, body.ContentAr, body.ContentEn);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Page_PolicyEdit)
        .WithName("UpdatePolicySection");

        policies.MapDelete("/sections/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeletePolicySectionCommand(id), ct).ConfigureAwait(false);
            return result.ToNoContentHttpResult();
        })
        .RequireAuthorization(Permissions.Page_PolicyEdit)
        .WithName("DeletePolicySection");

        return app;
    }
}

public sealed record UpdatePoliciesSettingsRequest(
    string RowVersion);

public sealed record CreatePolicySectionRequest(
    int Type,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn);

public sealed record UpdatePolicySectionRequest(
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn);
