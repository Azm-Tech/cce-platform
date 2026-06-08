using CCE.Api.Common.Extensions;
using CCE.Api.Common.Requests;
using CCE.Application.Content.Commands.SubmitCountryContentRequest;
using CCE.Application.Content.Queries.GetCountryContentRequest;
using CCE.Application.Content.Queries.ListCountryContentRequests;
using CCE.Application.Country.Commands.UpsertCountryProfile;
using CCE.Application.Country.Queries.GetMyCountryProfile;
using CCE.Domain;
using CCE.Domain.Content;
using CCE.Domain.Country;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class StateRepresentativeEndpoints
{
    public static IEndpointRouteBuilder MapStateRepresentativeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/state").WithTags("StateRepresentative");

        // US060
        group.MapGet("/profile", async (IMediator mediator, CancellationToken ct) =>
            (await mediator.Send(new GetMyCountryProfileQuery(), ct).ConfigureAwait(false)).ToHttpResult())
        .RequireAuthorization(Permissions.Country_Profile_Update)
        .WithName("GetMyCountryProfile");

        // US061
        group.MapPut("/profile/{countryId:guid}", async (
            System.Guid countryId, UpsertCountryProfileRequest body,
            IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new UpsertCountryProfileCommand(
                countryId,
                body.DescriptionAr, body.DescriptionEn,
                body.KeyInitiativesAr, body.KeyInitiativesEn,
                body.ContactInfoAr, body.ContactInfoEn,
                body.Population, body.AreaSqKm, body.GdpPerCapita, body.NdcAssetId);
            return (await mediator.Send(cmd, ct).ConfigureAwait(false)).ToHttpResult();
        })
        .RequireAuthorization(Permissions.Country_Profile_Update)
        .WithName("UpdateMyCountryProfile");

        // US051 — list
        group.MapGet("/requests", async (
            int? page, int? pageSize,
            CountryContentRequestStatus? status, ContentType? type,
            IMediator mediator, CancellationToken ct) =>
            (await mediator.Send(new ListCountryContentRequestsQuery(page ?? 1, pageSize ?? 20, status, type, null), ct)
                .ConfigureAwait(false)).ToHttpResult())
        .RequireAuthorization(Permissions.Content_Country_View)
        .WithName("ListMyCountryContentRequests");

        // US051 — single
        group.MapGet("/requests/{id:guid}", async (
            System.Guid id, IMediator mediator, CancellationToken ct) =>
            (await mediator.Send(new GetCountryContentRequestQuery(id), ct).ConfigureAwait(false)).ToHttpResult())
        .RequireAuthorization(Permissions.Content_Country_View)
        .WithName("GetMyCountryContentRequest");

        // US052 / US053 — unified submit
        group.MapPost("/requests", async (
            SubmitContentRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new SubmitCountryContentRequestCommand(
                body.Type, body.CountryId,
                body.TitleAr, body.TitleEn,
                body.DescriptionAr, body.DescriptionEn,
                body.ResourceType, body.AssetFileId,
                body.TopicId, body.FeaturedImageAssetId,
                body.StartsOn, body.EndsOn,
                body.LocationAr, body.LocationEn, body.OnlineMeetingUrl);
            return (await mediator.Send(cmd, ct).ConfigureAwait(false)).ToCreatedHttpResult();
        })
        .RequireAuthorization(Permissions.Content_Country_Submit)
        .WithName("SubmitCountryContentRequest");

        return app;
    }
}
