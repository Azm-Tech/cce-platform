using Refit;

namespace CCE.Integration.Communication;

/// <summary>
/// Refit client for the central email / SMS integration gateway.
/// Contract is generic — actual gateway paths and payloads can be
/// remapped via a custom <see cref="DelegatingHandler"/> if needed.
/// </summary>
public interface ICommunicationGatewayClient
{
    [Post("/api/v1/email/send")]
    Task<GatewayResponse> SendEmailAsync([Body] SendEmailRequest request, CancellationToken cancellationToken = default);

    [Post("/api/v1/sms/send")]
    Task<GatewayResponse> SendSmsAsync([Body] SendSmsRequest request, CancellationToken cancellationToken = default);
}
