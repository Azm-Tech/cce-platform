using CCE.Application.Notifications.Dtos;
using MediatR;

namespace CCE.Application.Notifications.Commands.UpdateNotificationTemplate;

public sealed record UpdateNotificationTemplateCommand(
    System.Guid Id,
    string SubjectAr,
    string SubjectEn,
    string BodyAr,
    string BodyEn,
    bool IsActive) : IRequest<NotificationTemplateDto?>;
