using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListPublicPostsInTopic;

public sealed record ListPublicPostsInTopicQuery(
    System.Guid TopicId,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<PublicPostDto>>;
