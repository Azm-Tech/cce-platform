using CCE.Application.Common.Interfaces;
using CCE.Integration.Communication;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Communication;

/// <summary>
/// <see cref="IEmailSender"/> implementation that delegates to the
/// integration gateway via <see cref="ICommunicationGatewayClient"/>.
/// </summary>
public sealed class GatewayEmailSender : IEmailSender
{
    private readonly ICommunicationGatewayClient _client;
    private readonly ILogger<GatewayEmailSender> _logger;

    public GatewayEmailSender(ICommunicationGatewayClient client, ILogger<GatewayEmailSender> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var request = new SendEmailRequest(to, subject, htmlBody);
        var response = await _client.SendEmailAsync(request, ct).ConfigureAwait(false);

        if (!response.Success)
        {
            _logger.LogError(
                "Gateway email send failed for {To} with subject {Subject}: {Error}",
                to, subject, response.Error);
            throw new InvalidOperationException($"Gateway email send failed: {response.Error}");
        }

        _logger.LogInformation(
            "Sent email via gateway to {To} with subject {Subject} (messageId {MessageId})",
            to, subject, response.MessageId);
    }
}
