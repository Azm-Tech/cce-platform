using CCE.Api.Common.Extensions;
using CCE.Application.Content.Public.Queries.GetShareLink;
using CCE.Domain.Content;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class SharePublicEndpoints
{
    public static IEndpointRouteBuilder MapSharePublicEndpoints(this IEndpointRouteBuilder app)
    {
        var share = app.MapGroup("/api/share").WithTags("Share");

        share.MapGet("/{type}/{id:guid}", async (
            ShareContentType type,
            System.Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(
                new GetShareLinkQuery(type, id),
                cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetShareLink");

        return app;
    }
}
