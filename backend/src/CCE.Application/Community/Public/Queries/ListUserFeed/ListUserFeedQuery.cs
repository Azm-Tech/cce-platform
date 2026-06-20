using CCE.Application.Common;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Community.Public.Queries.ListCommunityFeed;
using CCE.Application.Common.Pagination;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListUserFeed;

/// <summary>
/// Returns the authenticated user's personal home feed: posts from communities, topics, and
/// authors they follow. Normal-author posts come from the pre-fanned Redis sorted-set
/// <c>feed:user:{userId}</c>; celebrity/expert posts are merged from SQL at read time.
/// Falls back to a pure SQL query when the Redis key is cold.
/// Supports the same filters as the community feed (<see cref="ListCommunityFeedQuery"/>).
/// </summary>
public sealed record ListUserFeedQuery(
    System.Guid UserId,
    PostFeedSort Sort,
    System.Collections.Generic.IReadOnlyList<System.Guid> TagIds,
    System.Guid? CommunityId,
    System.Guid? TopicId,
    CCE.Domain.Community.PostType? PostType,
    int Page,
    int PageSize) : IRequest<Response<PagedResult<CommunityFeedItemDto>>>;
