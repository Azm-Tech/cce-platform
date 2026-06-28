using CCE.Application.Common;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Commands.CreateNotificationTemplate;

public sealed record CreateNotificationTemplateCommand(
    string Code,
    string SubjectAr,
    string SubjectEn,
    string BodyAr,
    string BodyEn,
    NotificationChannel Channel,
    string VariableSchemaJson) : IRequest<Response<System.Guid>>;
