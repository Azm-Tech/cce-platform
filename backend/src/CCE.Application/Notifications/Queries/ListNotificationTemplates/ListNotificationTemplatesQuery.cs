using CCE.Application.Common.Pagination;
using CCE.Application.Notifications.Dtos;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Queries.ListNotificationTemplates;

public sealed record ListNotificationTemplatesQuery(
    int Page = 1,
    int PageSize = 20,
    NotificationChannel? Channel = null,
    bool? IsActive = null) : IRequest<PagedResult<NotificationTemplateDto>>;
