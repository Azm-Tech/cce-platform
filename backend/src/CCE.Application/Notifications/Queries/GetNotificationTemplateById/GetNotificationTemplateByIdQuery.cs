using CCE.Application.Notifications.Dtos;
using MediatR;

namespace CCE.Application.Notifications.Queries.GetNotificationTemplateById;

public sealed record GetNotificationTemplateByIdQuery(System.Guid Id) : IRequest<NotificationTemplateDto?>;
