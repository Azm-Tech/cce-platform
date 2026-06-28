using CCE.Api.Common.Extensions;
using CCE.Application.CommunityLaws.Commands.CreateCommunityLawSection;
using CCE.Domain;
using CCE.Application.CommunityLaws.Commands.DeleteCommunityLawSection;
using CCE.Application.CommunityLaws.Commands.ReorderCommunityLawSection;
using CCE.Application.CommunityLaws.Commands.UpdateCommunityLawSection;
using CCE.Application.CommunityLaws.Queries.GetCommunityLaws;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class CommunityLawsEndpoints
{
    public static IEndpointRouteBuilder MapCommunityLawsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/community-laws")
            .WithTags("CommunityLaws")
            .RequireAuthorization(Permissions.CommunityLaws_Manage);

        group.MapGet("", async (ISender sender) =>
        {
            var result = await sender.Send(new GetCommunityLawsQuery());
            return result.ToHttpResult();
        })
        .WithName("GetAdminCommunityLaws");

        group.MapPost("/sections", async (CreateCommunityLawSectionRequest body, ISender sender) =>
        {
            var cmd = new CreateCommunityLawSectionCommand(
                body.TitleAr, body.TitleEn, body.ContentAr, body.ContentEn);
            var result = await sender.Send(cmd);
            return result.ToCreatedHttpResult();
        })
        .WithName("CreateCommunityLawSection");

        group.MapPut("/sections/{id:guid}", async (
            Guid id,
            UpdateCommunityLawSectionRequest body,
            ISender sender) =>
        {
            var cmd = new UpdateCommunityLawSectionCommand(
                id, body.TitleAr, body.TitleEn, body.ContentAr, body.ContentEn);
            var result = await sender.Send(cmd);
            return result.ToHttpResult();
        })
        .WithName("UpdateCommunityLawSection");

        group.MapPut("/sections/{id:guid}/order", async (
            Guid id,
            ReorderCommunityLawSectionRequest body,
            ISender sender) =>
        {
            var cmd = new ReorderCommunityLawSectionCommand(id, body.OrderIndex);
            var result = await sender.Send(cmd);
            return result.ToHttpResult();
        })
        .WithName("ReorderCommunityLawSection");

        group.MapDelete("/sections/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteCommunityLawSectionCommand(id));
            return result.ToHttpResult();
        })
        .WithName("DeleteCommunityLawSection");

        return app;
    }
}

public sealed record CreateCommunityLawSectionRequest(
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn);

public sealed record UpdateCommunityLawSectionRequest(
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn);

public sealed record ReorderCommunityLawSectionRequest(int OrderIndex);
