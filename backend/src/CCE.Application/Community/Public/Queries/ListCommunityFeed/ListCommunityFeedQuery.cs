using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListCommunityFeed;

/// <summary>
/// Community home feed (§A.1 read path). Global across public communities by default; optionally
/// scoped by <paramref name="CommunityId"/> and/or <paramref name="TopicId"/> and filtered by
/// <paramref name="TagIds"/> (matched by Id). Community-scoped Hot/Newest with no tag filter is
/// served from the Redis fan-out read-model; everything else falls back to SQL.
/// </summary>
public sealed record ListCommunityFeedQuery(
    PostFeedSort Sort,
    System.Collections.Generic.IReadOnlyList<System.Guid> TagIds,
    System.Guid? CommunityId,
    System.Guid? TopicId,
    System.Guid? UserId,
    CCE.Domain.Community.PostType? PostType,
    int Page,
    int PageSize) : IRequest<Response<PagedResult<CommunityFeedItemDto>>>;
