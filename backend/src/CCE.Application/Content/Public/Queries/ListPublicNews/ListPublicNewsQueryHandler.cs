using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicNews;

public sealed class ListPublicNewsQueryHandler : IRequestHandler<ListPublicNewsQuery, Response<PagedResult<PublicNewsDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;
    private readonly IUserContentInterestResolver _resolver;

    public ListPublicNewsQueryHandler(ICceDbContext db, MessageFactory messages, IUserContentInterestResolver resolver)
    {
        _db = db;
        _messages = messages;
        _resolver = resolver;
    }

    public async Task<Response<PagedResult<PublicNewsDto>>> Handle(ListPublicNewsQuery request, CancellationToken cancellationToken)
    {
        var knowledgeLevelId = request.KnowledgeLevelId;
        var jobSectorId = request.JobSectorId;

        (knowledgeLevelId, jobSectorId) = await _resolver.ResolveAsync(knowledgeLevelId, jobSectorId, cancellationToken).ConfigureAwait(false);

        var query = _db.News
            .Where(n => n.PublishedOn != null)
            .WhereIf(request.IsFeatured.HasValue, n => n.IsFeatured == request.IsFeatured!.Value)
            .WhereIf(request.TopicId.HasValue, n => n.TopicId == request.TopicId!.Value)
            .WhereIf(request.TagIds?.Count > 0, n => n.Tags.Any(t => request.TagIds!.Contains(t.Id)));

        if (knowledgeLevelId.HasValue || jobSectorId.HasValue)
        {
            query = query.Where(n =>
                (!knowledgeLevelId.HasValue || n.KnowledgeLevelId == null || n.KnowledgeLevelId == knowledgeLevelId.Value) &&
                (!jobSectorId.HasValue || n.JobSectorId == null || n.JobSectorId == jobSectorId.Value));

            query = query.OrderByDescending(n =>
                (knowledgeLevelId.HasValue && n.KnowledgeLevelId == knowledgeLevelId.Value ? 2 : 0) +
                (jobSectorId.HasValue && n.JobSectorId == jobSectorId.Value ? 1 : 0))
                .ThenByDescending(n => n.PublishedOn);
        }
        else
        {
            query = query.OrderByDescending(n => n.PublishedOn);
        }

        var result = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);

        var topicIds = result.Items.Select(n => n.TopicId).Distinct().ToList();
        var topicsList = await _db.Topics.Where(t => topicIds.Contains(t.Id))
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topicById = topicsList.ToDictionary(t => t.Id);

        var newsIds = result.Items.Select(n => n.Id).ToList();
        var tagByNewsId = await GetTagDtosByNewsIdsAsync(newsIds, cancellationToken).ConfigureAwait(false);

        return _messages.Ok(result.Map(n => MapToDto(n, topicById, tagByNewsId)), "ITEMS_LISTED");
    }

    private async Task<Dictionary<System.Guid, List<TagDto>>> GetTagDtosByNewsIdsAsync(
        System.Collections.Generic.List<System.Guid> newsIds, CancellationToken ct)
    {
        if (newsIds.Count == 0)
            return new Dictionary<System.Guid, List<TagDto>>();

        var entries = await _db.News
            .Where(n => newsIds.Contains(n.Id))
            .Select(n => new { n.Id, Tags = n.Tags.Select(t => new TagDto(t.Id, t.NameAr, t.NameEn, t.Color)).ToList() })
            .ToListAsyncEither(ct).ConfigureAwait(false);

        return entries.ToDictionary(x => x.Id, x => x.Tags);
    }

    internal static PublicNewsDto MapToDto(News n, Dictionary<System.Guid, Topic> topicById, Dictionary<System.Guid, List<TagDto>> tagByNewsId) => new(
        n.Id,
        n.TitleAr,
        n.TitleEn,
        n.ContentAr,
        n.ContentEn,
        n.TopicId,
        topicById.TryGetValue(n.TopicId, out var t) ? t.NameAr : string.Empty,
        topicById.TryGetValue(n.TopicId, out t) ? t.NameEn : string.Empty,
        n.FeaturedImageUrl,
        n.PublishedOn!.Value,
        n.IsFeatured,
        tagByNewsId.TryGetValue(n.Id, out var tags) ? tags : new List<TagDto>(),
        n.KnowledgeLevelId,
        n.JobSectorId);

}
