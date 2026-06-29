using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Queries.GetModerationQueue;

public sealed class GetModerationQueueQueryHandler
    : IRequestHandler<GetModerationQueueQuery, Response<PagedResult<ModerationQueueItemDto>>>
{
    private const int ContentPreviewLength = 120;

    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetModerationQueueQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PagedResult<ModerationQueueItemDto>>> Handle(
        GetModerationQueueQuery request,
        CancellationToken cancellationToken)
    {
        var statusFilter = !string.IsNullOrWhiteSpace(request.Status) &&
            System.Enum.TryParse<ModerationStatus>(request.Status, ignoreCase: true, out var s)
            ? (ModerationStatus?)s : null;

        var contentTypeFilter = !string.IsNullOrWhiteSpace(request.ContentType) &&
            System.Enum.TryParse<ModerationContentType>(request.ContentType, ignoreCase: true, out var ct)
            ? (ModerationContentType?)ct : null;

        var query = _db.ModerationRecords
            .WhereIf(statusFilter.HasValue,      r => r.Status      == statusFilter!.Value)
            .WhereIf(contentTypeFilter.HasValue, r => r.ContentType == contentTypeFilter!.Value)
            .OrderByDescending(r => r.CreatedOn);

        var paged = await query
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        if (paged.Items.Count == 0)
        {
            var empty = new PagedResult<ModerationQueueItemDto>(
                System.Array.Empty<ModerationQueueItemDto>(),
                paged.Page, paged.PageSize, paged.Total);
            return _msg.Ok(empty, MessageKeys.General.ITEMS_LISTED);
        }

        var postIds = paged.Items
            .Where(r => r.ContentType == ModerationContentType.Post)
            .Select(r => r.ContentId)
            .Distinct()
            .ToList();

        var replyIds = paged.Items
            .Where(r => r.ContentType == ModerationContentType.Reply)
            .Select(r => r.ContentId)
            .Distinct()
            .ToList();

        var postPreviews = postIds.Count > 0
            ? await _db.Posts.IgnoreQueryFilters()
                .Where(p => postIds.Contains(p.Id))
                .Select(p => new
                {
                    p.Id,
                    Preview = p.Content == null
                        ? string.Empty
                        : p.Content.Length > ContentPreviewLength
                            ? p.Content.Substring(0, ContentPreviewLength)
                            : p.Content,
                })
                .ToDictionaryAsync(p => p.Id, p => p.Preview, cancellationToken)
                .ConfigureAwait(false)
            : new System.Collections.Generic.Dictionary<System.Guid, string>();

        var replyPreviews = replyIds.Count > 0
            ? await _db.PostReplies.IgnoreQueryFilters()
                .Where(r => replyIds.Contains(r.Id))
                .Select(r => new
                {
                    r.Id,
                    Preview = r.Content.Length > ContentPreviewLength
                        ? r.Content.Substring(0, ContentPreviewLength)
                        : r.Content,
                })
                .ToDictionaryAsync(r => r.Id, r => r.Preview, cancellationToken)
                .ConfigureAwait(false)
            : new System.Collections.Generic.Dictionary<System.Guid, string>();

        var items = paged.Items.Select(r =>
        {
            var preview = r.ContentType == ModerationContentType.Post
                ? postPreviews.GetValueOrDefault(r.ContentId, string.Empty)
                : replyPreviews.GetValueOrDefault(r.ContentId, string.Empty);

            return new ModerationQueueItemDto(
                RecordId: r.Id,
                ContentType: r.ContentType,
                ContentId: r.ContentId,
                Status: r.Status,
                Phase: r.Phase,
                Provider: r.Provider,
                Score: r.Score,
                Category: r.Category,
                Reason: r.Reason,
                CreatedOn: r.CreatedOn,
                ContentPreview: preview);
        }).ToList();

        var result = new PagedResult<ModerationQueueItemDto>(
            items, paged.Page, paged.PageSize, paged.Total);
        return _msg.Ok(result, MessageKeys.General.ITEMS_LISTED);
    }
}
