using CCE.Application.Common.Pagination;
using CCE.Application.Notifications.Public.Dtos;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Public.Queries.ListMyNotifications;

public sealed record ListMyNotificationsQuery(
    System.Guid UserId,
    int Page = 1,
    int PageSize = 20,
    NotificationStatus? Status = null) : IRequest<PagedResult<UserNotificationDto>>;
