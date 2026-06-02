using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListHomepageFeed;

public sealed record ListHomepageFeedQuery(
    int Page = 1,
    int PageSize = 20,
    HomepageFeedContentType? ContentType = null,
    System.Guid? TopicId = null,
    HomepageFeedSortBy SortBy = HomepageFeedSortBy.Date,
    SortOrder SortOrder = SortOrder.Descending) : IRequest<Response<PagedResult<HomepageFeedItemDto>>>;
