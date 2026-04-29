using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Notifications.Dtos;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Queries.ListNotificationTemplates;

public sealed class ListNotificationTemplatesQueryHandler
    : IRequestHandler<ListNotificationTemplatesQuery, PagedResult<NotificationTemplateDto>>
{
    private readonly ICceDbContext _db;

    public ListNotificationTemplatesQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<NotificationTemplateDto>> Handle(
        ListNotificationTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<NotificationTemplate> query = _db.NotificationTemplates;

        if (request.Channel is { } channel)
        {
            query = query.Where(t => t.Channel == channel);
        }

        if (request.IsActive is { } isActive)
        {
            query = query.Where(t => t.IsActive == isActive);
        }

        query = query.OrderBy(t => t.Code);

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items.Select(MapToDto).ToList();
        return new PagedResult<NotificationTemplateDto>(items, page.Page, page.PageSize, page.Total);
    }

    internal static NotificationTemplateDto MapToDto(NotificationTemplate t) => new(
        t.Id,
        t.Code,
        t.SubjectAr,
        t.SubjectEn,
        t.BodyAr,
        t.BodyEn,
        t.Channel,
        t.VariableSchemaJson,
        t.IsActive);
}
