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
        var query = _db.Topics
            .Where(t => t.IsActive)
            .WhereIf(!string.IsNullOrWhiteSpace(search), t =>
                t.NameAr.Contains(search!) ||
                t.NameEn.Contains(search!) ||
                t.Slug.Contains(search!));

        if (request.SortBy == "postsCount")
        {
            var paged = await query
                .Select(t => new PublicTopicItemDto(
                    t.Id, t.NameAr, t.NameEn, t.Slug,
                    _db.Posts.Count(p => p.TopicId == t.Id && p.Status == PostStatus.Published)))
                .OrderByDescending(t => t.PostsCount)
                .ToPagedResultAsync(request.Page, request.PageSize, ct)
                .ConfigureAwait(false);

            return _messages.Ok(paged, "TOPICS_LISTED");
        }

        query = request.SortBy switch
        {
            "name" => query.OrderBy(t => t.NameAr),
            _ => query.OrderBy(t => t.OrderIndex),
        };

        var result = await query
            .Select(t => new PublicTopicItemDto(
                t.Id, t.NameAr, t.NameEn, t.Slug,
                _db.Posts.Count(p => p.TopicId == t.Id && p.Status == PostStatus.Published)))
            .ToPagedResultAsync(request.Page, request.PageSize, ct)
            .ConfigureAwait(false);

        return _messages.Ok(result, "TOPICS_LISTED");
    }
}
