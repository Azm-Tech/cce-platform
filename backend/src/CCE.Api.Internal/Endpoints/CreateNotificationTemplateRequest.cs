using CCE.Domain.Notifications;

namespace CCE.Api.Internal.Endpoints;

public sealed record CreateNotificationTemplateRequest(
    string Code,
    string SubjectAr,
    string SubjectEn,
    string BodyAr,
    string BodyEn,
    NotificationChannel Channel,
    string VariableSchemaJson);
