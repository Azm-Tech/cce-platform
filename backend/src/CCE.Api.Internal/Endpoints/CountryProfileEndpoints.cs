using CCE.Api.Common.Extensions;
using CCE.Api.Common.Requests;
using CCE.Application.Common;
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
            var result = await mediator.Send(new GetCountryProfileQuery(countryId), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Country_Profile_Update)
        .WithName("GetCountryProfile");

        group.MapPut("", async (
            System.Guid countryId,
            UpsertCountryProfileRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new UpsertCountryProfileCommand(
                countryId,
                body.DescriptionAr, body.DescriptionEn,
                body.KeyInitiativesAr, body.KeyInitiativesEn,
                body.ContactInfoAr, body.ContactInfoEn,
                body.Population, body.AreaSqKm, body.GdpPerCapita, body.NdcAssetId);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Country_Profile_Update)
        .WithName("UpsertCountryProfile");

        return app;
    }
}
