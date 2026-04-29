using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Notifications.Public.Dtos;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Public.Queries.ListMyNotifications;

public sealed class ListMyNotificationsQueryHandler
    : IRequestHandler<ListMyNotificationsQuery, PagedResult<UserNotificationDto>>
{
    private readonly ICceDbContext _db;

    public ListMyNotificationsQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<UserNotificationDto>> Handle(
        ListMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<UserNotification> query = _db.UserNotifications
            .Where(n => n.UserId == request.UserId);

        if (request.Status is { } status)
        {
            query = query.Where(n => n.Status == status);
        }

        query = query.OrderByDescending(n => n.SentOn).ThenByDescending(n => n.Id);

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items.Select(MapToDto).ToList();
        return new PagedResult<UserNotificationDto>(items, page.Page, page.PageSize, page.Total);
    }

    internal static UserNotificationDto MapToDto(UserNotification n) => new(
        n.Id,
        n.TemplateId,
        n.RenderedSubjectAr,
        n.RenderedSubjectEn,
        n.RenderedBody,
        n.RenderedLocale,
        n.Channel,
        n.SentOn,
        n.ReadOn,
        n.Status);
}
