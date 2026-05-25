using CCE.Api.Common.Auth;
using CCE.Api.Common.Extensions;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Auth.Register;
using CCE.Application.Identity.Public.Commands.SubmitExpertRequest;
using CCE.Application.Identity.Public.Commands.UpdateMyProfile;
using CCE.Application.Identity.Public.Queries.GetMyExpertStatus;
using CCE.Application.Identity.Public.Queries.GetMyProfile;
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

        // Compatibility route for older frontend calls. Sprint 01 local auth
        // owns registration now; it creates the user only and does not auto-login.
        users.MapPost("/register", async (
            RegisterUserRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new RegisterUserCommand(
                body.FirstName,
                body.LastName,
                body.EmailAddress,
                body.JobTitle,
                body.OrganizationName,
                body.PhoneNumber,
                body.Password,
                body.ConfirmPassword), ct).ConfigureAwait(false);
            return result.ToCreatedHttpResult();
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
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToCreatedHttpResult();
        })
        .WithName("SubmitExpertRequest");

        var me = app.MapGroup("/api/me").WithTags("Profile").RequireAuthorization();

        me.MapGet("", async (
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();
            var result = await mediator.Send(new GetMyProfileQuery(userId), ct).ConfigureAwait(false);
            return result.ToHttpResult();
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
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("UpdateMyProfile");

        me.MapGet("/expert-status", async (
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();
            var result = await mediator.Send(new GetMyExpertStatusQuery(userId), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("GetMyExpertStatus");

        return app;
    }
}
