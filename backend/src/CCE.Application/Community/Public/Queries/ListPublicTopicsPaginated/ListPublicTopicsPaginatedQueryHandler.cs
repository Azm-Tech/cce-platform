using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListPublicTopicsPaginated;

internal sealed class ListPublicTopicsPaginatedQueryHandler(
    ICceDbContext _db,
    MessageFactory _messages)
    : IRequestHandler<ListPublicTopicsPaginatedQuery, Response<PagedResult<PublicTopicItemDto>>>
{
    public async Task<Response<PagedResult<PublicTopicItemDto>>> Handle(
        ListPublicTopicsPaginatedQuery request, CancellationToken ct)
    {
        var search = request.Search;

        var baseQuery = _db.Topics
            .Where(t => t.IsActive)
            .WhereIf(!string.IsNullOrWhiteSpace(search), t =>
                t.NameAr.Contains(search!) ||
                t.NameEn.Contains(search!) ||
                t.Slug.Contains(search!));

        if (request.SortBy == TopicsSortBy.PostsCount)
        {
            var postCounts = await _db.Posts
                .Where(p => p.Status == PostStatus.Published)
                .GroupBy(p => p.TopicId)
                .Select(g => new { TopicId = g.Key, Count = g.Count() })
                .ToListAsyncEither(ct)
                .ConfigureAwait(false);

            var countMap = postCounts.ToDictionary(x => x.TopicId, x => x.Count);

            var allTopicIds = await baseQuery
                .Select(t => t.Id)
                .ToListAsyncEither(ct)
                .ConfigureAwait(false);

            var total = allTopicIds.Count;

            var pagedIds = allTopicIds
                .OrderByDescending(id => countMap.GetValueOrDefault(id, 0))
                .ThenBy(id => id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            if (pagedIds.Count == 0)
                return _messages.Ok(
                    new PagedResult<PublicTopicItemDto>([], request.Page, request.PageSize, total),
                    "TOPICS_LISTED");

            var topics = await _db.Topics
                .Where(t => pagedIds.Contains(t.Id))
                .Select(t => new { t.Id, t.NameAr, t.NameEn })
                .ToListAsyncEither(ct)
                .ConfigureAwait(false);

            var topicMap = topics.ToDictionary(t => t.Id);

            var sortedItems = pagedIds
                .Where(id => topicMap.ContainsKey(id))
                .Select(id => new PublicTopicItemDto(
                    id,
                    topicMap[id].NameAr,
                    topicMap[id].NameEn,
                    countMap.GetValueOrDefault(id, 0)))
                .ToList();

            return _messages.Ok(
                new PagedResult<PublicTopicItemDto>(sortedItems, request.Page, request.PageSize, total),
                "TOPICS_LISTED");
        }

        IQueryable<Topic> sortedQuery;
        if (request.SortBy == TopicsSortBy.Name)
            sortedQuery = baseQuery.OrderBy(t => t.NameAr);
        else
            sortedQuery = baseQuery.OrderBy(t => t.OrderIndex);

        var pagedIdsResult = await sortedQuery
            .Select(t => t.Id)
            .ToPagedResultAsync(request.Page, request.PageSize, ct)
            .ConfigureAwait(false);

        var topicIds = pagedIdsResult.Items.ToList();
        if (topicIds.Count == 0)
            return _messages.Ok(
                new PagedResult<PublicTopicItemDto>([], pagedIdsResult.Page, pagedIdsResult.PageSize, pagedIdsResult.Total),
                "TOPICS_LISTED");

        var pagedTopics = await _db.Topics
            .Where(t => topicIds.Contains(t.Id))
            .Select(t => new { t.Id, t.NameAr, t.NameEn })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);

        var pagedTopicMap = pagedTopics.ToDictionary(t => t.Id);

        var pagedPostCounts = await _db.Posts
            .Where(p => topicIds.Contains(p.TopicId) && p.Status == PostStatus.Published)
            .GroupBy(p => p.TopicId)
            .Select(g => new { TopicId = g.Key, Count = g.Count() })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);

        var pagedCountMap = pagedPostCounts.ToDictionary(x => x.TopicId, x => x.Count);

        var items = pagedIdsResult.Items
            .Where(id => pagedTopicMap.ContainsKey(id))
            .Select(id => new PublicTopicItemDto(
                id,
                pagedTopicMap[id].NameAr,
                pagedTopicMap[id].NameEn,
                pagedCountMap.GetValueOrDefault(id, 0)))
            .ToList();

        return _messages.Ok(
            new PagedResult<PublicTopicItemDto>(items, pagedIdsResult.Page, pagedIdsResult.PageSize, pagedIdsResult.Total),
            "TOPICS_LISTED");
    }
}
