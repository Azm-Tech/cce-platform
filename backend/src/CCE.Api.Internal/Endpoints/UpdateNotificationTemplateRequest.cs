namespace CCE.Api.Internal.Endpoints;

public sealed record UpdateNotificationTemplateRequest(
    string SubjectAr,
    string SubjectEn,
    string BodyAr,
    string BodyEn,
    bool IsActive);
