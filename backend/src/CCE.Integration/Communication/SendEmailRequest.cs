namespace CCE.Integration.Communication;

public sealed record SendEmailRequest(
    string To,
    string From,
    string Subject,
    string Html,
    string? TemplateId = null);
