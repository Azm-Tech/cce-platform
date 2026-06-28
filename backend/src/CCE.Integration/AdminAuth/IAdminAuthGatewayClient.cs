using Refit;

namespace CCE.Integration.AdminAuth;

public interface IAdminAuthGatewayClient
{
    [Post("/integrationgateway/auth/ad/login")]
    Task<AdAuthResponse> LoginAsync([Body] AdAuthRequest request, CancellationToken cancellationToken = default);
}
