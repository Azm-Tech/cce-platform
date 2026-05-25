using CCE.Application.Lookups.Queries.GetCountryCodeById;
using CCE.Application.Lookups.Queries.ListCountryCodes;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class CountryCodesPublicEndpoints
{
    public static IEndpointRouteBuilder MapCountryCodesPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/country-codes").WithTags("CountryCodes");

        group.MapGet("", async (
            string? search, bool? isActive,
            IMediator mediator, CancellationToken ct) =>
        {
            var query = new ListCountryCodesQuery(Search: search, IsActive: isActive);
            var result = await mediator.Send(query, ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("ListPublicCountryCodes");

        group.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCountryCodeByIdQuery(id), ct).ConfigureAwait(false);
            return result.Success ? Results.Ok(result) : Results.NotFound(result);
        })
        .AllowAnonymous()
        .WithName("GetPublicCountryCodeById");

        return app;
    }
}
