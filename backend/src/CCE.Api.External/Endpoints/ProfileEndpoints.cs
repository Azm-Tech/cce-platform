using CCE.Api.Common.Auth;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Public.Commands.SubmitExpertRequest;
using CCE.Application.Identity.Public.Commands.UpdateMyProfile;
using CCE.Application.Identity.Public.Queries.GetMyExpertStatus;
using CCE.Application.Identity.Public.Queries.GetMyProfile;
using CCE.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace CCE.Api.External.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/users").WithTags("Profile");

        users.MapPost("/register", (HttpContext ctx, IOptions<BffOptions> bffOpts) =>
        {
            var o = bffOpts.Value;
            var redirectUri = $"{ctx.Request.Scheme}://{ctx.Request.Host}/auth/callback";
            var url = $"{o.KeycloakBaseUrl}/realms/{o.KeycloakRealm}/protocol/openid-connect/registrations"
                + $"?client_id={System.Uri.EscapeDataString(o.KeycloakClientId)}"
                + $"&response_type=code"
                + $"&redirect_uri={System.Uri.EscapeDataString(redirectUri)}"
                + $"&scope={System.Uri.EscapeDataString("openid profile email")}";
            return Results.Redirect(url);
        })
        .AllowAnonymous()
        .WithName("RegisterUser");

        var usersAuth = app.MapGroup("/api/users").WithTags("Profile").RequireAuthorization();
        usersAuth.MapPost("/expert-request", async (
            SubmitExpertRequestRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();
            var cmd = new SubmitExpertRequestCommand(
                userId, body.RequestedBioAr, body.RequestedBioEn,
                body.RequestedTags ?? System.Array.Empty<string>());
            var dto = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return Results.Created("/api/me/expert-status", dto);
        })
        .WithName("SubmitExpertRequest");

        var me = app.MapGroup("/api/me").WithTags("Profile").RequireAuthorization();

        me.MapGet("", async (
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();
            var dto = await mediator.Send(new GetMyProfileQuery(userId), ct).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithName("GetMyProfile");

        me.MapPut("", async (
            UpdateMyProfileRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();
            var cmd = new UpdateMyProfileCommand(
                userId, body.LocalePreference, body.KnowledgeLevel,
                body.Interests ?? System.Array.Empty<string>(),
                body.AvatarUrl, body.CountryId);
            var dto = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithName("UpdateMyProfile");

        me.MapGet("/expert-status", async (
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();
            var dto = await mediator.Send(new GetMyExpertStatusQuery(userId), ct).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithName("GetMyExpertStatus");

        return app;
    }
}

public sealed record UpdateMyProfileRequest(
    string LocalePreference,
    KnowledgeLevel KnowledgeLevel,
    IReadOnlyList<string>? Interests,
    string? AvatarUrl,
    System.Guid? CountryId);

public sealed record SubmitExpertRequestRequest(
    string RequestedBioAr,
    string RequestedBioEn,
    IReadOnlyList<string>? RequestedTags);
