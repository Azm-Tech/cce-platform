using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.CreateNews;

public sealed record CreateNewsCommand(
    string TitleAr, string TitleEn,
    string ContentAr, string ContentEn,
    System.Guid TopicId,
    string? FeaturedImageUrl,
    System.Collections.Generic.IReadOnlyList<System.Guid>? TagIds = null,
    System.Guid? KnowledgeLevelId = null,
    System.Guid? JobSectorId = null) : IRequest<Response<NewsDto>>;
