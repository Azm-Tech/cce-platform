using CCE.Api.Common.Extensions;
using CCE.Application.InterestManagement.Queries.GetInterestQuestions;
using CCE.Application.InterestManagement.Queries.ListInterestTopics;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class InterestTopicPublicEndpoints
{
    public static IEndpointRouteBuilder MapInterestTopicPublicEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/interest-topics", async (
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new ListInterestTopicsQuery(), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("ListInterestTopicsPublic");

        app.MapGet("/api/interest-topics/questions", async (
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetInterestQuestionsQuery(), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("GetInterestQuestions");

        return app;
    }
}
