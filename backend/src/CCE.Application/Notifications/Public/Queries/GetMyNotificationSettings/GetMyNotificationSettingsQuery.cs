using CCE.Application.Common;
using CCE.Application.Notifications.Public.Dtos;
using MediatR;

namespace CCE.Application.Notifications.Public.Queries.GetMyNotificationSettings;

public sealed record GetMyNotificationSettingsQuery(System.Guid UserId)
    : IRequest<Response<IReadOnlyCollection<NotificationSettingsDto>>>;
