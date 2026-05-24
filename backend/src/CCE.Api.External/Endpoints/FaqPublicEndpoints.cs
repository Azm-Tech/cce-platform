using CCE.Api.Common.Extensions;
using CCE.Application.PlatformSettings.Public.Queries.GetPublicFaqs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class FaqPublicEndpoints
{
    public static IEndpointRouteBuilder MapFaqPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var faqs = app.MapGroup("/api/faqs").WithTags("FAQ");

        faqs.MapGet("", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPublicFaqsQuery(), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetPublicFaqs");

        return app;
    }
}
