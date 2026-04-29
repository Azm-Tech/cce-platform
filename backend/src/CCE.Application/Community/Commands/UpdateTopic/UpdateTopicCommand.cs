using CCE.Application.Community.Dtos;
using MediatR;

namespace CCE.Application.Community.Commands.UpdateTopic;

public sealed record UpdateTopicCommand(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string DescriptionAr,
    string DescriptionEn,
    int OrderIndex,
    bool IsActive) : IRequest<TopicDto?>;
