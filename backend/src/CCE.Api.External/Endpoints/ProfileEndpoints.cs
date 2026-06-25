using CCE.Api.Common.Auth;
using CCE.Api.Common.Extensions;
using CCE.Api.Common.Results;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Auth.Register;
using CCE.Application.Identity.Public.Commands.ConfirmEmailChange;
using CCE.Application.Identity.Public.Commands.ConfirmPhoneChange;
using CCE.Application.Identity.Public.Commands.RequestEmailChange;
using CCE.Application.Identity.Public.Commands.RequestPhoneChange;
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
                body.ConfirmPassword,
                body.CountryId), ct).ConfigureAwait(false);
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
            if (userId == System.Guid.Empty) return EnvelopeResults.Unauthorized();
            var cmd = new SubmitExpertRequestCommand(
                userId, body.RequestedBioAr, body.RequestedBioEn,
                body.RequestedTags ?? System.Array.Empty<string>(),
                body.CvAssetFileId);
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
            if (userId == System.Guid.Empty) return EnvelopeResults.Unauthorized();
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
            if (userId == System.Guid.Empty) return EnvelopeResults.Unauthorized();
            var cmd = new UpdateMyProfileCommand(
                userId,
                body.FirstName, body.LastName, body.JobTitle, body.OrganizationName,
                body.LocalePreference, body.KnowledgeLevel,
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
            if (userId == System.Guid.Empty) return EnvelopeResults.Unauthorized();
            var result = await mediator.Send(new GetMyExpertStatusQuery(userId), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("GetMyExpertStatus");

        me.MapPost("/email/request-change", async (
            RequestEmailChangeRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return EnvelopeResults.Unauthorized();
            var result = await mediator.Send(
                new RequestEmailChangeCommand(userId, body.NewEmail), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("RequestEmailChange");

        me.MapPost("/email/confirm-change", async (
            ConfirmEmailChangeRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return EnvelopeResults.Unauthorized();
            var result = await mediator.Send(
                new ConfirmEmailChangeCommand(userId, body.VerificationId, body.Code), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("ConfirmEmailChange");

        me.MapPost("/phone/request-change", async (
            RequestPhoneChangeRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return EnvelopeResults.Unauthorized();
            var result = await mediator.Send(
                new RequestPhoneChangeCommand(userId, body.NewPhone, body.CountryId), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("RequestPhoneChange");

        me.MapPost("/phone/confirm-change", async (
            ConfirmPhoneChangeRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator, CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return EnvelopeResults.Unauthorized();
            var result = await mediator.Send(
                new ConfirmPhoneChangeCommand(userId, body.VerificationId, body.Code), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("ConfirmPhoneChange");

        return app;
    }
}
