using CCE.Application.Community.Dtos;
using MediatR;

namespace CCE.Application.Community.Commands.CreateTopic;

public sealed record CreateTopicCommand(
    string NameAr,
    string NameEn,
    string DescriptionAr,
    string DescriptionEn,
    string Slug,
    System.Guid? ParentId,
    string? IconUrl,
    int OrderIndex) : IRequest<TopicDto>;
