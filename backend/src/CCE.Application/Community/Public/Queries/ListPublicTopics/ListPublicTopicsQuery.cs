using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListPublicTopics;

public sealed record ListPublicTopicsQuery() : IRequest<System.Collections.Generic.IReadOnlyList<PublicTopicDto>>;
