using CCE.Api.Common.Extensions;
using CCE.Application.Content.Commands.SubscribeNewsletter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CCE.Api.External.Endpoints.Newsletter;

public static class NewsletterEndpoints
{
    public static IEndpointRouteBuilder MapNewsletterEndpoints(this IEndpointRouteBuilder app)
    {
        var newsletter = app.MapGroup("/newsletter").WithTags("Newsletter");

        newsletter.MapPost("/subscribe", async (
            SubscribeNewsletterRequest req,
            [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new SubscribeNewsletterCommand(req.Email, ParseLocale(acceptLanguage));
            var result = await sender.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("SubscribeNewsletter");

        return app;
    }

    // Extracts the primary language tag and normalises to "ar" or "en".
    // Accept-Language can be e.g. "ar-SA,ar;q=0.9,en;q=0.8" — take the first tag only.
    private static string ParseLocale(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
            return "en";

        var primary = acceptLanguage.Split(',')[0].Split(';')[0].Trim();
        var lang = primary.Split('-')[0].ToLowerInvariant();
        return lang == "ar" ? "ar" : "en";
    }
}

internal sealed record SubscribeNewsletterRequest(string Email);
