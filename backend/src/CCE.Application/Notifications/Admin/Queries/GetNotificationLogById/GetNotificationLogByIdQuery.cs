using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Notifications.Admin.Queries.GetNotificationLogById;

public sealed record GetNotificationLogByIdQuery(System.Guid Id) : IRequest<Response<NotificationLogDto>>;
