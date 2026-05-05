using System.Threading.RateLimiting;
using CCE.Api.Common.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Api.Common.RateLimiting;

public sealed class TierOptions
{
    public int RequestsPerMinute { get; init; } = 120;
}

public static class TieredRateLimiterRegistration
{
    public static IServiceCollection AddCceTieredRateLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        var anon = configuration.GetSection("RateLimit:Anonymous").Get<TierOptions>() ?? new TierOptions { RequestsPerMinute = 120 };
        var auth = configuration.GetSection("RateLimit:Authenticated").Get<TierOptions>() ?? new TierOptions { RequestsPerMinute = 600 };
        var sw = configuration.GetSection("RateLimit:SearchAndWrite").Get<TierOptions>() ?? new TierOptions { RequestsPerMinute = 30 };

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsync("Too many requests", ct).ConfigureAwait(false);
            };

            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var (tier, partitionKey, perMinute) = Classify(httpContext, anon, auth, sw);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"{tier}:{partitionKey}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        Window = System.TimeSpan.FromMinutes(1),
                        PermitLimit = perMinute,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true,
                    });
            });
        });

        return services;
    }

    private static (string tier, string partitionKey, int perMinute) Classify(
        HttpContext ctx,
        TierOptions anon,
        TierOptions auth,
        TierOptions searchAndWrite)
    {
        var path = ctx.Request.Path.Value ?? string.Empty;
        var isSearchOrWrite = path.StartsWith("/api/search", System.StringComparison.OrdinalIgnoreCase)
            || !HttpMethods.IsGet(ctx.Request.Method);

        var hasAuth = ctx.Request.Headers.ContainsKey("Authorization")
            || ctx.Request.Cookies.ContainsKey(CceAuthCookies.SessionCookieName);

        if (isSearchOrWrite)
        {
            return ("sw", PartitionKey(ctx, hasAuth), searchAndWrite.RequestsPerMinute);
        }
        if (!hasAuth)
        {
            return ("anon", IpKey(ctx), anon.RequestsPerMinute);
        }
        return ("auth", PartitionKey(ctx, hasAuth: true), auth.RequestsPerMinute);
    }

    private static string PartitionKey(HttpContext ctx, bool hasAuth)
    {
        if (!hasAuth) return IpKey(ctx);
        // Prefer JWT sub from header; fallback to session cookie hash; fallback to IP.
        var auth = ctx.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
        {
            // Cheap: hash the token. For real partition keying we'd parse the sub claim,
            // but we don't want to validate JWTs in the limiter.
            return $"bearer:{auth.Length}:{auth.GetHashCode(System.StringComparison.Ordinal)}";
        }
        if (ctx.Request.Cookies.TryGetValue(CceAuthCookies.SessionCookieName, out var cookie) && !string.IsNullOrEmpty(cookie))
        {
            return $"sess:{cookie.Length}:{cookie.GetHashCode(System.StringComparison.Ordinal)}";
        }
        return IpKey(ctx);
    }

    private static string IpKey(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    public static IApplicationBuilder UseCceTieredRateLimiter(this IApplicationBuilder app)
        => app.UseRateLimiter();
}
