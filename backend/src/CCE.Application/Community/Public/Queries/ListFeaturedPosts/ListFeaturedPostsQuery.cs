using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListFeaturedPosts;

/// <summary>
/// Public feed of the most popular community posts, ranked by ratings then replies.
/// Optional <paramref name="TopicId"/> narrows to a single topic.
/// </summary>
public sealed record ListFeaturedPostsQuery(
    int Page = 1,
    int PageSize = 10,
    System.Guid? TopicId = null) : IRequest<Response<PagedResult<FeaturedPostDto>>>;
