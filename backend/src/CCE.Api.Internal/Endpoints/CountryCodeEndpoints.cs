using CCE.Application.Lookups.Commands.UpsertCountryCode;
using CCE.Application.Lookups.Queries.GetCountryCodeById;
using CCE.Application.Lookups.Queries.ListCountryCodes;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class CountryCodeEndpoints
{
    public static IEndpointRouteBuilder MapCountryCodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/country-codes").WithTags("CountryCodes");

        group.MapGet("", async (
            string? search, bool? isActive,
            IMediator mediator, CancellationToken ct) =>
        {
            var query = new ListCountryCodesQuery(Search: search, IsActive: isActive);
            var result = await mediator.Send(query, ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.Lookup_Manage)
        .WithName("ListCountryCodes");

        group.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCountryCodeByIdQuery(id), ct).ConfigureAwait(false);
            return result.Success ? Results.Ok(result) : Results.NotFound(result);
        })
        .RequireAuthorization(Permissions.Lookup_Manage)
        .WithName("GetCountryCodeById");

        group.MapPost("", async (
            UpsertCountryCodeRequest body,
            IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new UpsertCountryCodeCommand(
                body.Id,
                body.NameAr,
                body.NameEn,
                body.DialCode,
                body.FlagUrl,
                body.IsActive);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .RequireAuthorization(Permissions.Lookup_Manage)
        .WithName("UpsertCountryCode");

        return app;
    }
}

public sealed record UpsertCountryCodeRequest(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string DialCode,
    string? FlagUrl,
    bool IsActive);
