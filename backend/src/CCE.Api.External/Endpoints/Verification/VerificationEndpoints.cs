using CCE.Api.Common.Extensions;
using CCE.Application.Verification.Commands.RequestVerification;
using CCE.Application.Verification.Commands.VerifyOtp;
using MediatR;

namespace CCE.Api.External.Endpoints.Verification;

public static class VerificationEndpoints
{
    public static IEndpointRouteBuilder MapVerificationEndpoints(this IEndpointRouteBuilder app)
    {
        var verification = app.MapGroup("/verification").WithTags("Verification");

        verification.MapPost("/request", async (
            RequestVerificationRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new RequestVerificationCommand(
                req.Token, req.ProviderName, req.Contact, req.TypeId);
            var result = await sender.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("RequestVerification");

        verification.MapPost("/verify", async (
            VerifyOtpRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new VerifyOtpCommand(req.VerificationId, req.Code);
            var result = await sender.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("VerifyOtp");

        return app;
    }
}
