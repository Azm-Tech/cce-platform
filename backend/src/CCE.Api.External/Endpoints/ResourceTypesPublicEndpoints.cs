using CCE.Api.Common.Extensions;
using CCE.Application.Content.Public.Queries.ListResourceTypes;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class ResourceTypesPublicEndpoints
{
    public static IEndpointRouteBuilder MapResourceTypesPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/resource-types").WithTags("ResourceTypes");

        group.MapGet("/", ListResourceTypes)
            .AllowAnonymous()
            .WithName("ListResourceTypes");

        return app;
    }

    private static async Task<IResult> ListResourceTypes(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListResourceTypesQuery(), cancellationToken).ConfigureAwait(false);
        return result.ToHttpResult();
    }
}
