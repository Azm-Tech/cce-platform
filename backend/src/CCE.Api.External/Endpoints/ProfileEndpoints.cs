using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Public.Commands.SubmitExpertRequest;
using CCE.Application.Identity.Public.Commands.UpdateMyProfile;
using CCE.Application.Identity.Public.Queries.GetMyExpertStatus;
using CCE.Application.Identity.Public.Queries.GetMyProfile;
using CCE.Domain.Identity;
using CCE.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/users").WithTags("Profile");

        // Sub-11 Phase 01 — admin-driven user creation via Microsoft Graph.
        // Was a GET-redirect-to-Keycloak-registration-page; now a POST that
        // calls EntraIdRegistrationService. Anonymous self-service deferred
        // to Sub-11d (needs IEmailSender abstraction to deliver temp password).
        users.MapPost("/register", async (
            RegisterUserRequest body,
            EntraIdRegistrationService registrationService,
            CancellationToken ct) =>
        {
            if (body is null
                || string.IsNullOrWhiteSpace(body.GivenName)
                || string.IsNullOrWhiteSpace(body.Surname)
                || string.IsNullOrWhiteSpace(body.Email)
                || string.IsNullOrWhiteSpace(body.MailNickname))
            {
                return Results.BadRequest(new { error = "GivenName, Surname, Email, MailNickname are required." });
            }
            var dto = new RegistrationRequest(body.GivenName, body.Surname, body.Email, body.MailNickname);
            try
            {
                var result = await registrationService.CreateUserAsync(dto, ct).ConfigureAwait(false);
                return Results.Created($"/api/users/{result.EntraIdObjectId}", result);
            }
            catch (EntraIdRegistrationConflictException)
            {
                return Results.Conflict(new { error = "User principal name already exists in Entra ID." });
            }
            catch (EntraIdRegistrationAuthorizationException)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("cce-admin"))
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

public sealed record RegisterUserRequest(
    string GivenName,
    string Surname,
    string Email,
    string MailNickname);
