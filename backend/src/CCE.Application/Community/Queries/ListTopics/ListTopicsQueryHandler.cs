using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Queries.ListTopics;

public sealed class ListTopicsQueryHandler
    : IRequestHandler<ListTopicsQuery, Response<PagedResult<TopicDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public ListTopicsQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<PagedResult<TopicDto>>> Handle(
        ListTopicsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<Topic> query = _db.Topics;

        if (request.ParentId is { } parentId)
        {
            query = query.Where(t => t.ParentId == parentId);
        }

        if (request.IsActive is { } isActive)
        {
            query = query.Where(t => t.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(t =>
                t.NameAr.Contains(search) ||
                t.NameEn.Contains(search) ||
                t.Slug.Contains(search));
        }

        query = query.OrderBy(t => t.OrderIndex);

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return _messages.Ok(page.Map(MapToDto), MessageKeys.General.ITEMS_LISTED);
    }

    internal static TopicDto MapToDto(Topic t) => new(
        t.Id,
        t.NameAr,
        t.NameEn,
        t.DescriptionAr,
        t.DescriptionEn,
        t.Slug,
        t.ParentId,
        t.IconUrl,
        t.OrderIndex,
        t.IsActive);
}
