using CCE.Application.Kapsarc;
using CCE.Integration.Kapsarc;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Kapsarc;

/// <summary>
/// <see cref="IKapsarcClient"/> implementation that delegates to the KAPSARC
/// integration gateway via <see cref="IKapsarcGatewayClient"/> (Refit).
/// Network / gateway failures are swallowed into an <c>Unavailable</c> result so the
/// caller can surface BRD ER001 rather than a raw exception.
/// </summary>
public sealed class GatewayKapsarcClient : IKapsarcClient
{
    private readonly IKapsarcGatewayClient _client;
    private readonly ILogger<GatewayKapsarcClient> _logger;

    public GatewayKapsarcClient(IKapsarcGatewayClient client, ILogger<GatewayKapsarcClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<KapsarcClassificationResult> GetClassificationAsync(
        string countryCode, string countryName, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetClassificationAsync(countryCode, countryName, ct).ConfigureAwait(false);

            if (!"success".Equals(response.Status, StringComparison.OrdinalIgnoreCase)
                || response.Classification is null
                || response.PerformanceScore is null
                || response.TotalIndex is null)
            {
                _logger.LogWarning(
                    "KAPSARC returned no data for {CountryCode}: {Error}",
                    countryCode, response.Error ?? "incomplete payload");
                return KapsarcClassificationResult.Unavailable(response.Error ?? "KAPSARC data unavailable");
            }

            return KapsarcClassificationResult.Ok(
                response.Classification,
                response.PerformanceScore.Value,
                response.TotalIndex.Value);
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogError(ex, "KAPSARC classification lookup failed for {CountryCode}", countryCode);
            return KapsarcClassificationResult.Unavailable(ex.Message);
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            _logger.LogError(ex, "KAPSARC classification lookup failed for {CountryCode}", countryCode);
            return KapsarcClassificationResult.Unavailable(ex.Message);
        }
        catch (System.TimeoutException ex)
        {
            _logger.LogError(ex, "KAPSARC classification lookup timed out for {CountryCode}", countryCode);
            return KapsarcClassificationResult.Unavailable(ex.Message);
        }
    }
}
