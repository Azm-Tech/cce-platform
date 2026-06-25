using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Community.Public.Queries.ListCommunityFeed;
using MediatR;

namespace CCE.Application.Community.Public.Queries.SearchCommunityPosts;

/// <summary>
/// Full-text community search. Dispatched by <c>GET /api/community/feed?searchTerm=</c> when
/// <c>searchTerm</c> is non-empty. Results are hydrated through <see cref="FeedHydratorService"/>
/// so the response shape is identical to <see cref="ListCommunityFeed.ListCommunityFeedQuery"/>,
/// extended with four nullable highlight fields on <see cref="CommunityFeedItemDto"/>.
/// When <paramref name="Sort"/> is null, results are ordered by Meilisearch relevance rank;
/// when a sort value is provided, SQL-level ordering is applied over the candidate ID set.
/// </summary>
public sealed record SearchCommunityPostsQuery(
    string SearchTerm,
    PostFeedSort? Sort,
    System.Collections.Generic.IReadOnlyList<System.Guid> TagIds,
    System.Guid? CommunityId,
    System.Guid? TopicId,
    System.Guid? UserId,
    CCE.Domain.Community.PostType? PostType,
    int Page,
    int PageSize,
    System.Guid? AuthorId = null) : IRequest<Response<PagedResult<CommunityFeedItemDto>>>;
