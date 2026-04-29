using CCE.Application.Country.Commands.UpdateCountry;
using CCE.Application.Country.Queries.GetCountryById;
using CCE.Application.Country.Queries.ListCountries;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class CountryEndpoints
{
    public static IEndpointRouteBuilder MapCountryEndpoints(this IEndpointRouteBuilder app)
    {
        var countries = app.MapGroup("/api/admin/countries").WithTags("Countries");

        countries.MapGet("", async (
            int? page, int? pageSize, string? search, bool? isActive,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListCountriesQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                Search: search,
                IsActive: isActive);
            var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.Country_Profile_Update)
        .WithName("ListCountries");

        countries.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var dto = await mediator.Send(new GetCountryByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Country_Profile_Update)
        .WithName("GetCountryById");

        countries.MapPut("/{id:guid}", async (
            System.Guid id,
            UpdateCountryRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new UpdateCountryCommand(
                id,
                body.NameAr, body.NameEn,
                body.RegionAr, body.RegionEn,
                body.IsActive);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Country_Profile_Update)
        .WithName("UpdateCountry");

        return app;
    }
}

public sealed record UpdateCountryRequest(
    string NameAr,
    string NameEn,
    string RegionAr,
    string RegionEn,
    bool IsActive);
