using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.InteractiveMaps.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.InteractiveMaps.Public.Queries.GetInteractiveMapNodeDetails;

internal sealed class GetInteractiveMapNodeDetailsQueryHandler
    : IRequestHandler<GetInteractiveMapNodeDetailsQuery, Response<MapNodeDetailsDto>>
{
    private const int SliceSize = 5;

    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetInteractiveMapNodeDetailsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<MapNodeDetailsDto>> Handle(
        GetInteractiveMapNodeDetailsQuery request,
        CancellationToken cancellationToken)
    {
        // ─── 1. Resolve the node ───
        var node = await _db.InteractiveMapNodes
            .AsNoTracking()
            .Where(n => n.Id == request.NodeId && n.IsActive)
            .Select(n => new { n.Id, n.NameAr, n.NameEn, n.IconKey, n.TopicId })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (node is null)
            return _msg.NodeNotFound<MapNodeDetailsDto>();

        // ─── 1b. Resolve node tag IDs for tag-based matching ───
        var nodeTagIds = await _db.InteractiveMapNodes
            .AsNoTracking()
            .Where(n => n.Id == request.NodeId && n.IsActive)
            .SelectMany(n => n.Tags.Select(t => t.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var hasTags = nodeTagIds.Count > 0;

        // ─── 2. Resolve the linked topic ───
        var topic = await _db.Topics
            .AsNoTracking()
            .Where(t => t.Id == node.TopicId && t.IsActive)
            .Select(t => new { t.Id, t.NameAr, t.NameEn, t.DescriptionAr, t.DescriptionEn, t.Slug, t.IconUrl })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (topic is null)
            return _msg.MapNotFound<MapNodeDetailsDto>();

        // ─── 3. News — top N by topic or tags, newest first ───
        var news = await _db.News
            .AsNoTracking()
            .Where(n => n.PublishedOn != null)
            .Where(n => n.TopicId == node.TopicId || (hasTags && n.Tags.Any(t => nodeTagIds.Contains(t.Id))))
            .OrderByDescending(n => n.PublishedOn)
            .Take(SliceSize)
            .Select(n => new MapNodeNewsDto(n.Id, n.TitleAr, n.TitleEn, n.FeaturedImageUrl, n.PublishedOn!.Value))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // ─── 4. Events — upcoming by topic or tags, soonest first ───
        var now = DateTimeOffset.UtcNow;
        var events = await _db.Events
            .AsNoTracking()
            .Where(e => e.StartsOn >= now)
            .Where(e => e.TopicId == node.TopicId || (hasTags && e.Tags.Any(t => nodeTagIds.Contains(t.Id))))
            .OrderBy(e => e.StartsOn)
            .Take(SliceSize)
            .Select(e => new MapNodeEventDto(e.Id, e.TitleAr, e.TitleEn, e.StartsOn, e.EndsOn, e.FeaturedImageUrl))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // ─── 5. Posts — published by topic or tags, hottest first ───
        var posts = await _db.Posts
            .AsNoTracking()
            .Where(p => p.Status == PostStatus.Published)
            .Where(p => p.TopicId == node.TopicId || (hasTags && p.Tags.Any(t => nodeTagIds.Contains(t.Id))))
            .OrderByDescending(p => p.Score)
            .Take(SliceSize)
            .Select(p => new MapNodePostDto(p.Id, p.Type, p.Title, p.Content, p.CommentsCount, p.CreatedOn))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // ─── 6. Resources — top N recently published (Resource has no TopicId FK) ───
        var categoryIds = await _db.Resources
            .AsNoTracking()
            .Where(r => r.PublishedOn != null)
            .OrderByDescending(r => r.PublishedOn)
            .Take(SliceSize)
            .Select(r => new { r.Id, r.TitleAr, r.TitleEn, r.ResourceType, r.CategoryId, r.PublishedOn })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var catIds = categoryIds.Select(r => r.CategoryId).Distinct().ToList();
        var categories = await _db.ResourceCategories
            .AsNoTracking()
            .Where(c => catIds.Contains(c.Id))
            .Select(c => new { c.Id, c.NameAr, c.NameEn })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var catMap = categories.ToDictionary(c => c.Id);

        var resources = categoryIds
            .Select(r =>
            {
                var cat = catMap.GetValueOrDefault(r.CategoryId);
                return new MapNodeResourceDto(
                    r.Id, r.TitleAr, r.TitleEn, r.ResourceType,
                    cat?.NameAr ?? string.Empty,
                    cat?.NameEn ?? string.Empty,
                    r.PublishedOn!.Value);
            })
            .ToList();

        // ─── 7. Assemble ───
        var dto = new MapNodeDetailsDto(
            Node: new MapNodeSummaryDto(node.Id, node.NameAr, node.NameEn, node.IconKey, node.TopicId),
            Topic: new MapNodeTopicDto(topic.Id, topic.NameAr, topic.NameEn, topic.DescriptionAr, topic.DescriptionEn, topic.Slug, topic.IconUrl),
            Resources: resources,
            News: news,
            Events: events,
            Posts: posts);

        return _msg.Ok(dto, MessageKeys.General.ITEMS_LISTED);
    }
}
