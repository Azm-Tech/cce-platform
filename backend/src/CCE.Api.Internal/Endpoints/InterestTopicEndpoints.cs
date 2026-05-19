using CCE.Api.Common.Extensions;
using CCE.Application.InterestManagement.Commands.CreateInterestTopic;
using CCE.Application.InterestManagement.Commands.DeleteInterestTopic;
using CCE.Application.InterestManagement.Commands.UpdateInterestTopic;
using CCE.Application.InterestManagement.Queries.GetInterestTopicById;
using CCE.Application.InterestManagement.Queries.ListInterestTopics;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class InterestTopicEndpoints
{
    public static IEndpointRouteBuilder MapInterestTopicEndpoints(this IEndpointRouteBuilder app)
    {
        var topics = app.MapGroup("/api/admin/interest-topics").WithTags("Interest Topics");

        topics.MapGet("", async (
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new ListInterestTopicsQuery(), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InterestTopic_Manage)
        .WithName("ListInterestTopics");

        topics.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetInterestTopicByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InterestTopic_Manage)
        .WithName("GetInterestTopicById");

        topics.MapPost("", async (
            CreateInterestTopicRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new CreateInterestTopicCommand(body.NameAr, body.NameEn), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InterestTopic_Manage)
        .WithName("CreateInterestTopic");

        topics.MapPut("/{id:guid}", async (
            System.Guid id,
            UpdateInterestTopicRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new UpdateInterestTopicCommand(id, body.NameAr, body.NameEn), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InterestTopic_Manage)
        .WithName("UpdateInterestTopic");

        topics.MapDelete("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new DeleteInterestTopicCommand(id), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InterestTopic_Manage)
        .WithName("DeleteInterestTopic");

        return app;
    }
}

public sealed record CreateInterestTopicRequest(string NameAr, string NameEn);
public sealed record UpdateInterestTopicRequest(string NameAr, string NameEn);
