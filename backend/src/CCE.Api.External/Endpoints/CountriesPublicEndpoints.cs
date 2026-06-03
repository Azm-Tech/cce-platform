using CCE.Api.Common.Extensions;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.CountryPublic.Queries.GetPublicCountryProfile;
using CCE.Application.CountryPublic.Queries.ListPublicCountries;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Country;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CCE.Api.External.Endpoints;

public static class CountriesPublicEndpoints
{
    public static IEndpointRouteBuilder MapCountriesPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var countries = app.MapGroup("/api/countries").WithTags("CountriesPublic");

        // US014 AC3 — browsable country list for profile selection
        countries.MapGet("", async (
            string? search, int? page, int? pageSize,
            PublicCountrySortBy? sortBy, SortOrder? sortOrder,
            IMediator mediator, CancellationToken ct) =>
            (await mediator.Send(new ListPublicCountriesQuery(
                search, page ?? 1, pageSize ?? 20,
                sortBy ?? PublicCountrySortBy.TotalIndex,
                sortOrder ?? SortOrder.Descending), ct)
                .ConfigureAwait(false)).ToHttpResult())
        .AllowAnonymous()
        .WithName("ListPublicCountries");

        // US014 AC4-6 — full state profile including KAPSARC metrics and NDC document info
        countries.MapGet("/{id:guid}/profile", async (
            System.Guid id, IMediator mediator, CancellationToken ct) =>
            (await mediator.Send(new GetPublicCountryProfileQuery(id), ct)
                .ConfigureAwait(false)).ToHttpResult())
        .AllowAnonymous()
        .WithName("GetPublicCountryProfile");

        // US014 AC5 — download the Nationally Determined Contribution PDF for a country
        countries.MapGet("/{id:guid}/ndc", async (
            System.Guid id,
            HttpContext httpContext,
            ICceDbContext db,
            IFileStorage storage,
            CancellationToken ct) =>
        {
            // Resolve country → profile → NDC asset in one chain
            var country = await db.Countries
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, ct).ConfigureAwait(false);
            if (country is null)
                return Results.NotFound();

            var profile = await db.CountryProfiles
                .FirstOrDefaultAsync(p => p.CountryId == id, ct).ConfigureAwait(false);
            if (profile?.NationallyDeterminedContributionAssetId is null)
                return Results.NotFound();

            var asset = await db.AssetFiles
                .FirstOrDefaultAsync(a => a.Id == profile.NationallyDeterminedContributionAssetId.Value, ct)
                .ConfigureAwait(false);
            if (asset is null)
                return Results.NotFound();
            if (asset.VirusScanStatus != VirusScanStatus.Clean)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            httpContext.Response.ContentType = asset.MimeType;
            httpContext.Response.Headers.ContentDisposition =
                $"inline; filename=\"{System.Net.WebUtility.UrlEncode(asset.OriginalFileName)}\"";

            await using var stream = await storage.OpenReadAsync(asset.Url, ct).ConfigureAwait(false);
            await stream.CopyToAsync(httpContext.Response.Body, ct).ConfigureAwait(false);

            return Results.Empty;
        })
        .AllowAnonymous()
        .WithName("DownloadCountryNdc");

        return app;
    }
}
