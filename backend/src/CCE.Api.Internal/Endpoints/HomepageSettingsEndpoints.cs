using CCE.Api.Common.Extensions;
using CCE.Application.Common;
using CCE.Application.PlatformSettings.Commands.UpdateHomepageSettings;
using CCE.Application.PlatformSettings.Queries.GetHomepageSettings;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class HomepageSettingsEndpoints
{
    public static IEndpointRouteBuilder MapHomepageSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var settings = app.MapGroup("/api/admin/settings/homepage").WithTags("PlatformSettings");

        settings.MapGet("", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetHomepageSettingsQuery(), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("GetHomepageSettings");

        settings.MapPut("", async (UpdateHomepageSettingsRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var rowVersion = string.IsNullOrEmpty(body.RowVersion)
                ? System.Array.Empty<byte>()
                : System.Convert.FromBase64String(body.RowVersion);
            var cmd = new UpdateHomepageSettingsCommand(
                body.VideoUrl,
                body.ObjectiveAr,
                body.ObjectiveEn,
                body.CceConceptsAr,
                body.CceConceptsEn,
                body.ParticipatingCountryIds,
                rowVersion);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("UpdateHomepageSettings");

        return app;
    }
}

public sealed record UpdateHomepageSettingsRequest(
    string? VideoUrl,
    string ObjectiveAr,
    string ObjectiveEn,
    string CceConceptsAr,
    string CceConceptsEn,
    System.Collections.Generic.IReadOnlyList<System.Guid> ParticipatingCountryIds,
    string RowVersion);
