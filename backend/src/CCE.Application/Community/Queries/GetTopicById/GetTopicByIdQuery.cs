using CCE.Application.Community.Dtos;
using MediatR;

namespace CCE.Application.Community.Queries.GetTopicById;

public sealed record GetTopicByIdQuery(System.Guid Id) : IRequest<TopicDto?>;
