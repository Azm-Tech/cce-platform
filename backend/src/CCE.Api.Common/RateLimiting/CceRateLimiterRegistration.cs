using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

namespace CCE.Api.Common.RateLimiting;

public static class CceRateLimiterRegistration
{
    public const string AnonymousPolicy = "anonymous";

    /// <summary>
    /// Registers the CCE rate limiter with the anonymous policy applied as the global limiter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="testLimit">Override the per-window limit (test/dev only). Production uses 60.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCceRateLimiter(this IServiceCollection services, int testLimit = 60) =>
        services.AddRateLimiter(opts =>
        {
            opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = testLimit,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });
}
