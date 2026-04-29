using CCE.Domain.Notifications;

namespace CCE.Application.Notifications.Dtos;

public sealed record NotificationTemplateDto(
    System.Guid Id,
    string Code,
    string SubjectAr,
    string SubjectEn,
    string BodyAr,
    string BodyEn,
    NotificationChannel Channel,
    string VariableSchemaJson,
    bool IsActive);
