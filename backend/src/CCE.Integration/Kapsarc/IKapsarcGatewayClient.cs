using Refit;

namespace CCE.Integration.Kapsarc;

/// <summary>
/// Refit client for the KAPSARC (Saudi Energy Efficiency Center) Circular Carbon
/// Economy classification-verification service (BRD §6.5.1 / US014).
/// Data-retrieval contract: given a country's ISO code + name, returns its
/// CCE classification, performance and total index.
/// </summary>
public interface IKapsarcGatewayClient
{
    [Get("/integrationgateway/kapsarc/classification")]
    Task<KapsarcVerificationResponse> GetClassificationAsync(
        [AliasAs("countryCode")] string countryCode,
        [AliasAs("countryName")] string countryName,
        CancellationToken cancellationToken = default);
}
