namespace CCE.Integration.Communication;

public sealed record GatewayResponse(
    string Status,
    string? Id = null,
    string? Error = null);
