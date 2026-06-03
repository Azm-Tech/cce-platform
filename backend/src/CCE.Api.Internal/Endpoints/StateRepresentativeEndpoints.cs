using CCE.Api.Common.Extensions;
using CCE.Application.Content.Commands.SubmitCountryEventRequest;
using CCE.Application.Content.Commands.SubmitCountryNewsRequest;
using CCE.Application.Content.Commands.SubmitCountryResourceRequest;
using CCE.Application.Content.Queries.GetCountryContentRequest;
using CCE.Application.Content.Queries.ListCountryContentRequests;
using CCE.Application.Country.Commands.UpsertCountryProfile;
using CCE.Application.Country.Queries.GetMyCountryProfile;
using CCE.Domain;
using CCE.Domain.Country;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

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
            CountryContentRequestStatus? status, ContentKind? kind,
            IMediator mediator, CancellationToken ct) =>
            (await mediator.Send(new ListCountryContentRequestsQuery(page ?? 1, pageSize ?? 20, status, kind, null), ct)
                .ConfigureAwait(false)).ToHttpResult())
        .RequireAuthorization(Permissions.Content_Country_View)
        .WithName("ListMyCountryContentRequests");

        // US051 — single
        group.MapGet("/requests/{id:guid}", async (
            System.Guid id, IMediator mediator, CancellationToken ct) =>
            (await mediator.Send(new GetCountryContentRequestQuery(id), ct).ConfigureAwait(false)).ToHttpResult())
        .RequireAuthorization(Permissions.Content_Country_View)
        .WithName("GetMyCountryContentRequest");

        // US052
        group.MapPost("/requests/resource", async (
            SubmitResourceRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new SubmitCountryResourceRequestCommand(
                body.CountryId, body.TitleAr, body.TitleEn,
                body.DescriptionAr, body.DescriptionEn,
                body.ResourceType, body.AssetFileId);
            return (await mediator.Send(cmd, ct).ConfigureAwait(false)).ToCreatedHttpResult();
        })
        .RequireAuthorization(Permissions.Content_Country_Submit)
        .WithName("SubmitCountryResourceRequest");

        // US053 — news
        group.MapPost("/requests/news", async (
            SubmitNewsRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new SubmitCountryNewsRequestCommand(
                body.CountryId, body.TitleAr, body.TitleEn,
                body.ContentAr, body.ContentEn,
                body.TopicId, body.FeaturedImageAssetId);
            return (await mediator.Send(cmd, ct).ConfigureAwait(false)).ToCreatedHttpResult();
        })
        .RequireAuthorization(Permissions.Content_Country_Submit)
        .WithName("SubmitCountryNewsRequest");

        // US053 — event
        group.MapPost("/requests/event", async (
            SubmitEventRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new SubmitCountryEventRequestCommand(
                body.CountryId, body.TitleAr, body.TitleEn,
                body.DescriptionAr, body.DescriptionEn,
                body.TopicId, body.StartsOn, body.EndsOn,
                body.LocationAr, body.LocationEn, body.OnlineMeetingUrl);
            return (await mediator.Send(cmd, ct).ConfigureAwait(false)).ToCreatedHttpResult();
        })
        .RequireAuthorization(Permissions.Content_Country_Submit)
        .WithName("SubmitCountryEventRequest");

        return app;
    }
}

