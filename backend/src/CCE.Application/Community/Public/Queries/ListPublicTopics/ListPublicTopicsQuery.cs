using CCE.Application.Common;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListPublicTopics;

public sealed record ListPublicTopicsQuery() : IRequest<Response<System.Collections.Generic.IReadOnlyList<PublicTopicDto>>>;
