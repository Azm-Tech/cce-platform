using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

namespace CCE.Api.Common.RateLimiting;

public static class CceRateLimiterRegistration
{
    public const string AnonymousPolicy = "anonymous";
    private const int DefaultLimit = 60;

    /// <summary>
    /// Registers the CCE rate limiter with the anonymous policy applied as the global limiter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="testLimit">Override the per-window limit (test/dev only). Production uses 60.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCceRateLimiter(this IServiceCollection services, int testLimit = DefaultLimit) =>
        AddCceRateLimiterCore(services, testLimit);

    /// <summary>
    /// Configuration-driven overload. Reads <c>RateLimiter:PermitLimit</c> from the supplied
    /// configuration; falls back to <see cref="DefaultLimit"/> (60) if absent. Used by
    /// load-test runs that set <c>RATE_LIMITER__PERMITLIMIT=10000000</c> via env.
    /// </summary>
    public static IServiceCollection AddCceRateLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        var limit = configuration.GetValue<int?>("RateLimiter:PermitLimit") ?? DefaultLimit;
        return AddCceRateLimiterCore(services, limit);
    }

    private static IServiceCollection AddCceRateLimiterCore(IServiceCollection services, int permitLimit) =>
        services.AddRateLimiter(opts =>
        {
            opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = permitLimit,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });
}
