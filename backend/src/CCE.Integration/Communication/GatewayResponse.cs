namespace CCE.Integration.Communication;

public sealed record GatewayResponse(
    bool Success,
    string? MessageId = null,
    string? Error = null);
