using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Admin.Queries.ListNotificationLogs;

public sealed class ListNotificationLogsQueryHandler
    : IRequestHandler<ListNotificationLogsQuery, Response<PagedResult<NotificationLogListItemDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListNotificationLogsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PagedResult<NotificationLogListItemDto>>> Handle(
        ListNotificationLogsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<NotificationLog> query = _db.NotificationLogs;

        if (request.RecipientUserId is { } userId)
            query = query.Where(l => l.RecipientUserId == userId);

        if (!string.IsNullOrWhiteSpace(request.TemplateCode))
            query = query.Where(l => l.TemplateCode == request.TemplateCode);

        if (request.Channel is { } channel)
            query = query.Where(l => l.Channel == channel);

        if (request.Status is { } status)
            query = query.Where(l => l.Status == status);

        query = query.OrderByDescending(l => l.CreatedOn).ThenByDescending(l => l.Id);

        var page = await query.ToPagedResultAsync(
            request.Page,
            request.PageSize,
            cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items.Select(MapToDto).ToList();
        var result = new PagedResult<NotificationLogListItemDto>(items, page.Page, page.PageSize, page.Total);
        return _msg.Ok(result, MessageKeys.General.ITEMS_LISTED);
    }

    internal static NotificationLogListItemDto MapToDto(NotificationLog l) => new(
        l.Id,
        l.RecipientUserId,
        l.TemplateCode,
        l.TemplateId,
        l.Channel,
        l.Status,
        l.ProviderMessageId,
        l.Error,
        l.AttemptCount,
        l.CreatedOn,
        l.SentOn,
        l.FailedOn,
        l.CorrelationId);
}
