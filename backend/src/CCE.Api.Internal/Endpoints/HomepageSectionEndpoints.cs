using CCE.Application.Content.Commands.CreateHomepageSection;
using CCE.Application.Content.Commands.DeleteHomepageSection;
using CCE.Application.Content.Commands.UpdateHomepageSection;
using CCE.Application.Content.Queries.ListHomepageSections;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class HomepageSectionEndpoints
{
    public static IEndpointRouteBuilder MapHomepageSectionEndpoints(this IEndpointRouteBuilder app)
    {
        var sections = app.MapGroup("/api/admin/homepage-sections").WithTags("HomepageSections");

        sections.MapGet("", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new ListHomepageSectionsQuery(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("ListHomepageSections");

        sections.MapPost("", async (CreateHomepageSectionRequest body, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new CreateHomepageSectionCommand(body.SectionType, body.OrderIndex, body.ContentAr, body.ContentEn);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/admin/homepage-sections/{dto.Id}", dto);
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("CreateHomepageSection");

        sections.MapPut("/{id:guid}", async (
            System.Guid id,
            UpdateHomepageSectionRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new UpdateHomepageSectionCommand(id, body.ContentAr, body.ContentEn, body.IsActive);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("UpdateHomepageSection");

        sections.MapDelete("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new DeleteHomepageSectionCommand(id), cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("DeleteHomepageSection");

        return app;
    }
}

public sealed record CreateHomepageSectionRequest(
    CCE.Domain.Content.HomepageSectionType SectionType,
    int OrderIndex,
    string ContentAr,
    string ContentEn);

public sealed record UpdateHomepageSectionRequest(
    string ContentAr,
    string ContentEn,
    bool IsActive);
