using CCE.Api.Common.Extensions;
using CCE.Application.Common;
using CCE.Application.PlatformSettings.Commands.CreateFaq;
using CCE.Application.PlatformSettings.Commands.DeleteFaq;
using CCE.Application.PlatformSettings.Commands.UpdateFaq;
using CCE.Application.PlatformSettings.Queries.GetFaqById;
using CCE.Application.PlatformSettings.Queries.GetFaqs;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class FaqEndpoints
{
    public static IEndpointRouteBuilder MapFaqEndpoints(this IEndpointRouteBuilder app)
    {
        var faqs = app.MapGroup("/api/admin/settings/faqs").WithTags("PlatformSettings");

        faqs.MapGet("", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetFaqsQuery(), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Page_PolicyEdit)
        .WithName("GetFaqs");

        faqs.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetFaqByIdQuery(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Page_PolicyEdit)
        .WithName("GetFaqById");

        faqs.MapPost("", async (
            CreateFaqRequest body,
            IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new CreateFaqCommand(
                body.QuestionAr, body.QuestionEn,
                body.AnswerAr, body.AnswerEn,
                body.Order);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToCreatedHttpResult();
        })
        .RequireAuthorization(Permissions.Page_PolicyEdit)
        .WithName("CreateFaq");

        faqs.MapPut("/{id:guid}", async (
            System.Guid id,
            UpdateFaqRequest body,
            IMediator mediator, CancellationToken ct) =>
        {
            var cmd = new UpdateFaqCommand(
                id,
                body.QuestionAr, body.QuestionEn,
                body.AnswerAr, body.AnswerEn,
                body.Order);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Page_PolicyEdit)
        .WithName("UpdateFaq");

        faqs.MapDelete("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteFaqCommand(id), ct).ConfigureAwait(false);
            return result.ToNoContentHttpResult();
        })
        .RequireAuthorization(Permissions.Page_PolicyEdit)
        .WithName("DeleteFaq");

        return app;
    }
}

public sealed record CreateFaqRequest(
    string QuestionAr,
    string QuestionEn,
    string AnswerAr,
    string AnswerEn,
    int Order = 0);

public sealed record UpdateFaqRequest(
    string QuestionAr,
    string QuestionEn,
    string AnswerAr,
    string AnswerEn,
    int Order);
