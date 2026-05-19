namespace CCE.Integration.Communication;

public sealed record SendEmailRequest(
    string To,
    string Subject,
    string Body);
