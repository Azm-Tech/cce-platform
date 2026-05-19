using CCE.Application.Common.Interfaces;
using CCE.Infrastructure.Email;
using CCE.Integration.Communication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Communication;

/// <summary>
/// <see cref="IEmailSender"/> implementation that delegates to the
/// integration gateway via <see cref="ICommunicationGatewayClient"/>.
/// </summary>
public sealed class GatewayEmailSender : IEmailSender
{
    private readonly ICommunicationGatewayClient _client;
    private readonly IOptions<EmailOptions> _options;
    private readonly ILogger<GatewayEmailSender> _logger;

    public GatewayEmailSender(
        ICommunicationGatewayClient client,
        IOptions<EmailOptions> options,
        ILogger<GatewayEmailSender> logger)
    {
        _client = client;
        _options = options;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, string? templateId = null, CancellationToken ct = default)
    {
        var request = new SendEmailRequest(
            To: to,
            From: _options.Value.FromAddress,
            Subject: subject,
            Html: htmlBody,
            TemplateId: templateId);

        var response = await _client.SendEmailAsync(request, ct).ConfigureAwait(false);

        if (!"success".Equals(response.Status, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "Gateway email send failed for {To} with subject {Subject}: {Error}",
                to, subject, response.Error);
            throw new InvalidOperationException($"Gateway email send failed: {response.Error}");
        }

        _logger.LogInformation(
            "Sent email via gateway to {To} with subject {Subject} (id {Id})",
            to, subject, response.Id);
    }
}
