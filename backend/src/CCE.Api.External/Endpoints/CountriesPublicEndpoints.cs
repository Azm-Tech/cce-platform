using CCE.Application.CountryPublic.Queries.GetPublicCountryProfile;
using CCE.Application.CountryPublic.Queries.ListPublicCountries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class CountriesPublicEndpoints
{
    public static IEndpointRouteBuilder MapCountriesPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var countries = app.MapGroup("/api/countries").WithTags("CountriesPublic");

        countries.MapGet("", async (string? search, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListPublicCountriesQuery(search), ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("ListPublicCountries");

        countries.MapGet("/{id:guid}/profile", async (System.Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetPublicCountryProfileQuery(id), ct).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .AllowAnonymous()
        .WithName("GetPublicCountryProfile");

        return app;
    }
}
