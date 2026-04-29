using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Dtos;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Queries.ListTopics;

public sealed class ListTopicsQueryHandler
    : IRequestHandler<ListTopicsQuery, PagedResult<TopicDto>>
{
    private readonly ICceDbContext _db;

    public ListTopicsQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<TopicDto>> Handle(
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

        var items = page.Items.Select(MapToDto).ToList();
        return new PagedResult<TopicDto>(items, page.Page, page.PageSize, page.Total);
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
