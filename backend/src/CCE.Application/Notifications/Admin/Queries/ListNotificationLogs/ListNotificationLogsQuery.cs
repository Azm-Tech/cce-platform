using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Admin.Queries.ListNotificationLogs;

public sealed record ListNotificationLogsQuery(
    int Page,
    int PageSize,
    System.Guid? RecipientUserId = null,
    string? TemplateCode = null,
    NotificationChannel? Channel = null,
    NotificationDeliveryStatus? Status = null) : IRequest<Response<PagedResult<NotificationLogListItemDto>>>;
