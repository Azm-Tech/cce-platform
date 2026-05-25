namespace CCE.Integration.Communication;

public sealed record SendSmsRequest(
    string To,
    string Message);
