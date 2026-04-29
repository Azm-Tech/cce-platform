using CCE.Application.Country.Commands.UpsertCountryProfile;
using CCE.Application.Country.Queries.GetCountryProfile;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class CountryProfileEndpoints
{
    public static IEndpointRouteBuilder MapCountryProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/countries/{countryId:guid}/profile").WithTags("Countries");

        group.MapGet("", async (
            System.Guid countryId, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var dto = await mediator.Send(new GetCountryProfileQuery(countryId), cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Country_Profile_Update)
        .WithName("GetCountryProfile");

        group.MapPut("", async (
            System.Guid countryId,
            UpsertCountryProfileRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var rowVersion = string.IsNullOrEmpty(body.RowVersion)
                ? System.Array.Empty<byte>()
                : System.Convert.FromBase64String(body.RowVersion);
            var cmd = new UpsertCountryProfileCommand(
                countryId, body.DescriptionAr, body.DescriptionEn,
                body.KeyInitiativesAr, body.KeyInitiativesEn,
                body.ContactInfoAr, body.ContactInfoEn,
                rowVersion);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Country_Profile_Update)
        .WithName("UpsertCountryProfile");

        return app;
    }
}

public sealed record UpsertCountryProfileRequest(
    string DescriptionAr,
    string DescriptionEn,
    string KeyInitiativesAr,
    string KeyInitiativesEn,
    string? ContactInfoAr,
    string? ContactInfoEn,
    string RowVersion);
