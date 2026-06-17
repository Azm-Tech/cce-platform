using System.Linq;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetMyTopics;

/// <summary>
/// Returns the topics followed by the authenticated user, with the number of published posts
/// under each topic. Supports pagination and optional search by topic name.
/// </summary>
public sealed class GetMyTopicsQueryHandler
    : IRequestHandler<GetMyTopicsQuery, Response<PagedResult<MyTopicDto>>>
{
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly MessageFactory _msg;

    public GetMyTopicsQueryHandler(ICceDbContext db, ICurrentUserAccessor currentUser, MessageFactory msg)
    {
        _db = db;
        _currentUser = currentUser;
        _msg = msg;
    }

    public async Task<Response<PagedResult<MyTopicDto>>> Handle(
        GetMyTopicsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == System.Guid.Empty)
            return _msg.NotAuthenticated<PagedResult<MyTopicDto>>();

        // Step 1: paginate followed topic IDs (with search filter)
        var query = from f in _db.TopicFollows
                    join t in _db.Topics on f.TopicId equals t.Id
                    where f.UserId == userId.Value
                       && t.IsActive
                    select t;

        query = query
            .WhereIf(!string.IsNullOrWhiteSpace(request.Search),
                t => t.NameAr.Contains(request.Search!) || t.NameEn.Contains(request.Search!))
            .OrderBy(t => t.OrderIndex);

        var pagedIds = await query
            .Select(t => t.Id)
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var topicIds = pagedIds.Items.ToList();
        if (topicIds.Count == 0)
        {
            return _msg.Ok(
                new PagedResult<MyTopicDto>(System.Array.Empty<MyTopicDto>(), pagedIds.Page, pagedIds.PageSize, pagedIds.Total),
                "ITEMS_LISTED");
        }

        // Step 2: batch-load topic names
        var topics = await _db.Topics
            .Where(t => topicIds.Contains(t.Id))
            .Select(t => new { t.Id, t.NameAr, t.NameEn })
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var topicMap = topics.ToDictionary(t => t.Id);

        // Step 3: batch-count published posts per topic
        var postCounts = await _db.Posts
            .Where(p => topicIds.Contains(p.TopicId) && p.Status == PostStatus.Published)
            .GroupBy(p => p.TopicId)
            .Select(g => new { TopicId = g.Key, Count = g.Count() })
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var countMap = postCounts.ToDictionary(x => x.TopicId, x => x.Count);

        // Step 4: build DTOs preserving the paged order
        var items = pagedIds.Items
            .Where(id => topicMap.ContainsKey(id))
            .Select(id => new MyTopicDto(
                id,
                topicMap[id].NameAr,
                topicMap[id].NameEn,
                IsWatchlisted: true,
                countMap.GetValueOrDefault(id, 0)))
            .ToList();

        return _msg.Ok(
            new PagedResult<MyTopicDto>(items, pagedIds.Page, pagedIds.PageSize, pagedIds.Total),
            "ITEMS_LISTED");
    }
}
