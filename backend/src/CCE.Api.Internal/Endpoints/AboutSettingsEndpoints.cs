using CCE.Api.Common.Extensions;
using CCE.Application.Common;
using CCE.Application.PlatformSettings.Commands.CreateGlossaryEntry;
using CCE.Application.PlatformSettings.Commands.CreateKnowledgePartner;
using CCE.Application.PlatformSettings.Commands.DeleteGlossaryEntry;
using CCE.Application.PlatformSettings.Commands.DeleteKnowledgePartner;
using CCE.Application.PlatformSettings.Commands.UpdateAboutSettings;
using CCE.Application.PlatformSettings.Commands.UpdateGlossaryEntry;
using CCE.Application.PlatformSettings.Commands.UpdateKnowledgePartner;
using CCE.Application.PlatformSettings.Queries.GetAboutSettings;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class AboutSettingsEndpoints
{
    public static IEndpointRouteBuilder MapAboutSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var about = app.MapGroup("/api/admin/settings/about").WithTags("PlatformSettings");

        about.MapGet("", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetAboutSettingsQuery(), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("GetAboutSettings");

        about.MapPut("", async (UpdateAboutSettingsRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var rowVersion = string.IsNullOrEmpty(body.RowVersion)
                ? System.Array.Empty<byte>()
                : System.Convert.FromBase64String(body.RowVersion);
            var cmd = new UpdateAboutSettingsCommand(
                body.DescriptionAr, body.DescriptionEn,
                body.HowToUseVideoUrl, rowVersion);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("UpdateAboutSettings");

        about.MapPost("/glossary", async (CreateGlossaryEntryRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new CreateGlossaryEntryCommand(
                body.TermAr, body.TermEn, body.DefinitionAr, body.DefinitionEn);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToCreatedHttpResult();
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("CreateGlossaryEntry");

        about.MapPut("/glossary/{id:guid}", async (
            System.Guid id,
            UpdateGlossaryEntryRequest body,
            IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new UpdateGlossaryEntryCommand(
                id, body.TermAr, body.TermEn, body.DefinitionAr, body.DefinitionEn);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("UpdateGlossaryEntry");

        about.MapDelete("/glossary/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteGlossaryEntryCommand(id), ct).ConfigureAwait(false);
            return result.ToNoContentHttpResult();
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("DeleteGlossaryEntry");

        about.MapPost("/knowledge-partners", async (
            CreateKnowledgePartnerRequest body,
            IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new CreateKnowledgePartnerCommand(
                body.NameAr, body.NameEn, body.LogoUrl, body.WebsiteUrl,
                body.DescriptionAr, body.DescriptionEn);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToCreatedHttpResult();
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("CreateKnowledgePartner");

        about.MapPut("/knowledge-partners/{id:guid}", async (
            System.Guid id,
            UpdateKnowledgePartnerRequest body,
            IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new UpdateKnowledgePartnerCommand(
                id, body.NameAr, body.NameEn, body.LogoUrl, body.WebsiteUrl,
                body.DescriptionAr, body.DescriptionEn);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("UpdateKnowledgePartner");

        about.MapDelete("/knowledge-partners/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteKnowledgePartnerCommand(id), ct).ConfigureAwait(false);
            return result.ToNoContentHttpResult();
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("DeleteKnowledgePartner");

        return app;
    }
}

public sealed record UpdateAboutSettingsRequest(
    string DescriptionAr,
    string DescriptionEn,
    string? HowToUseVideoUrl,
    string RowVersion);

public sealed record CreateGlossaryEntryRequest(
    string TermAr,
    string TermEn,
    string DefinitionAr,
    string DefinitionEn);

public sealed record UpdateGlossaryEntryRequest(
    string TermAr,
    string TermEn,
    string DefinitionAr,
    string DefinitionEn);

public sealed record CreateKnowledgePartnerRequest(
    string NameAr,
    string NameEn,
    string? LogoUrl,
    string? WebsiteUrl,
    string? DescriptionAr,
    string? DescriptionEn);

public sealed record UpdateKnowledgePartnerRequest(
    string NameAr,
    string NameEn,
    string? LogoUrl,
    string? WebsiteUrl,
    string? DescriptionAr,
    string? DescriptionEn);
