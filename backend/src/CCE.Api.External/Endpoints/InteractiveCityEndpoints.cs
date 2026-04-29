using CCE.Application.Common.Interfaces;
using CCE.Application.InteractiveCity.Public.Commands.DeleteMyScenario;
using CCE.Application.InteractiveCity.Public.Commands.RunScenario;
using CCE.Application.InteractiveCity.Public.Commands.SaveScenario;
using CCE.Application.InteractiveCity.Public.Queries.ListCityTechnologies;
using CCE.Application.InteractiveCity.Public.Queries.ListMyScenarios;
using CCE.Domain.InteractiveCity;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class InteractiveCityEndpoints
{
    public static IEndpointRouteBuilder MapInteractiveCityEndpoints(this IEndpointRouteBuilder app)
    {
        // ─── Anonymous / public ───
        var pub = app.MapGroup("/api/interactive-city").WithTags("InteractiveCity");

        pub.MapGet("/technologies", async (
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new ListCityTechnologiesQuery(), cancellationToken)
                .ConfigureAwait(false);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("ListCityTechnologies");

        pub.MapPost("/scenarios/run", async (
            RunScenarioRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new RunScenarioCommand(body.CityType, body.TargetYear, body.ConfigurationJson);
            var result = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("RunScenario");

        // ─── Authenticated (my scenarios) ───
        var me = app.MapGroup("/api/me/interactive-city").WithTags("InteractiveCity");

        me.MapPost("/scenarios", async (
            SaveScenarioRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var userId = currentUser.GetUserId()
                ?? throw new System.InvalidOperationException("User identity required.");
            var cmd = new SaveScenarioCommand(
                userId, body.NameAr, body.NameEn, body.CityType, body.TargetYear, body.ConfigurationJson);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/me/interactive-city/scenarios/{dto.Id}", dto);
        })
        .RequireAuthorization()
        .WithName("SaveMyScenario");

        me.MapGet("/scenarios", async (
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var userId = currentUser.GetUserId()
                ?? throw new System.InvalidOperationException("User identity required.");
            var result = await mediator.Send(new ListMyScenariosQuery(userId), cancellationToken)
                .ConfigureAwait(false);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithName("ListMyScenarios");

        me.MapDelete("/scenarios/{id:guid}", async (
            System.Guid id,
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var userId = currentUser.GetUserId()
                ?? throw new System.InvalidOperationException("User identity required.");
            try
            {
                await mediator.Send(new DeleteMyScenarioCommand(id, userId), cancellationToken)
                    .ConfigureAwait(false);
                return Results.NoContent();
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithName("DeleteMyScenario");

        return app;
    }

    // ─── Request body records ───
    private sealed record RunScenarioRequest(CityType CityType, int TargetYear, string ConfigurationJson);
    private sealed record SaveScenarioRequest(string NameAr, string NameEn, CityType CityType, int TargetYear, string ConfigurationJson);
}
