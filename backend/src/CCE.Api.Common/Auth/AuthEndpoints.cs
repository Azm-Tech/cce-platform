using CCE.Api.Common.Extensions;
using CCE.Application.Identity.Auth.Common;
using CCE.Application.Identity.Auth.ForgotPassword;
using CCE.Application.Identity.Auth.Login;
using CCE.Application.Identity.Auth.Logout;
using CCE.Application.Identity.Auth.RefreshToken;
using CCE.Application.Identity.Auth.Register;
using CCE.Application.Identity.Auth.ResetPassword;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Common.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app, LocalAuthApi api)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/register", async (RegisterUserRequest body, IMediator mediator, CancellationToken ct) =>
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
        .WithName($"{api}RegisterUser");

        auth.MapPost("/login", async (LoginRequest body, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new LoginCommand(
                body.EmailAddress,
                body.Password,
                api,
                GetIpAddress(ctx),
                ctx.Request.Headers.UserAgent.ToString()), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName($"{api}Login");

        auth.MapPost("/refresh", async (RefreshTokenRequest body, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RefreshTokenCommand(
                body.RefreshToken,
                api,
                GetIpAddress(ctx),
                ctx.Request.Headers.UserAgent.ToString()), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName($"{api}RefreshToken");

        auth.MapPost("/forgot-password", async (ForgotPasswordRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ForgotPasswordCommand(body.EmailAddress), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName($"{api}ForgotPassword");

        auth.MapPost("/reset-password", async (ResetPasswordRequest body, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ResetPasswordCommand(
                body.EmailAddress,
                body.Token,
                body.NewPassword,
                body.ConfirmPassword,
                GetIpAddress(ctx)), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName($"{api}ResetPassword");

        auth.MapPost("/logout", async (LogoutRequest body, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new LogoutCommand(
                body.RefreshToken,
                GetIpAddress(ctx)), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName($"{api}Logout");

        return app;
    }

    private static string? GetIpAddress(HttpContext ctx)
        => ctx.Connection.RemoteIpAddress?.ToString();
}
