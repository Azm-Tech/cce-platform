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

        // Sub-11d — anonymous self-service registration via Microsoft Graph.
        // Sub-11 Phase 01 made this admin-only as a stop-gap until an
        // IEmailSender existed; Sub-11d Task A added the abstraction +
        // Task B wired it into EntraIdRegistrationService, so the temp
        // password is now delivered via email instead of returned in the
        // response. Endpoint is anonymous again — the welcome email is
        // the user's only credential channel.
        //
        // Response shape: 201 with the new user's UPN + objectId only.
        // The temporary password is intentionally NOT in the response
        // (would leak to logs / screen-captures); operators check the
        // email transport on registration failure.
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
                var response = new RegisterUserResponse(
                    result.EntraIdObjectId,
                    result.UserPrincipalName,
                    result.DisplayName);
                return Results.Created($"/api/users/{result.EntraIdObjectId}", response);
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

public sealed record RegisterUserRequest(
    string GivenName,
    string Surname,
    string Email,
    string MailNickname);

/// <summary>
/// Sub-11d — public response shape for /api/users/register. Excludes
/// the temporary password (delivered via the welcome email instead).
/// </summary>
public sealed record RegisterUserResponse(
    System.Guid EntraIdObjectId,
    string UserPrincipalName,
    string DisplayName);
