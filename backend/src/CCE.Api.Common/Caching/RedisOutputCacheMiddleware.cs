using System.Text;
using System.Text.Json;
using CCE.Api.Common.Auth;
using CCE.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CCE.Api.Common.Caching;

public sealed class RedisOutputCacheMiddleware
{
    private const string KeyPrefix = "out:";

    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly IOptions<OutputCacheOptions> _opts;
    private readonly IOptions<CceInfrastructureOptions> _infraOpts;
    private readonly ILogger<RedisOutputCacheMiddleware> _logger;

    public RedisOutputCacheMiddleware(
        RequestDelegate next,
        IConnectionMultiplexer redis,
        IOptions<OutputCacheOptions> opts,
        IOptions<CceInfrastructureOptions> infraOpts,
        ILogger<RedisOutputCacheMiddleware> logger)
    {
        _next = next;
        _redis = redis;
        _opts = opts;
        _infraOpts = infraOpts;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (!ShouldCache(ctx))
        {
            await _next(ctx).ConfigureAwait(false);
            return;
        }

        var key = BuildKey(ctx);
        var db = _redis.GetDatabase();
        var hit = await db.StringGetAsync(key).ConfigureAwait(false);
        if (hit.HasValue)
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<Envelope>(hit.ToString());
                if (envelope is not null)
                {
                    ctx.Response.ContentType = envelope.ContentType;
                    var bytes = System.Convert.FromBase64String(envelope.Body);
                    ctx.Response.StatusCode = StatusCodes.Status200OK;
                    await ctx.Response.Body.WriteAsync(bytes).ConfigureAwait(false);
                    return;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Cache envelope deserialization failed for {Key}; bypassing.", key);
            }
        }

        // No cache hit — capture response into a memory stream while letting downstream write to it.
        var originalBody = ctx.Response.Body;
        await using var capture = new MemoryStream();
        ctx.Response.Body = capture;
        try
        {
            await _next(ctx).ConfigureAwait(false);
            capture.Position = 0;
            var captured = capture.ToArray();

            // Only cache successful responses (2xx).
            if (ctx.Response.StatusCode >= 200 && ctx.Response.StatusCode < 300)
            {
                var envelope = new Envelope(ctx.Response.ContentType ?? "application/octet-stream", System.Convert.ToBase64String(captured));
                var ttl = System.TimeSpan.FromSeconds(_infraOpts.Value.OutputCacheTtlSeconds);
                await db.StringSetAsync(key, JsonSerializer.Serialize(envelope), ttl).ConfigureAwait(false);
            }

            await originalBody.WriteAsync(captured).ConfigureAwait(false);
        }
        finally
        {
            ctx.Response.Body = originalBody;
        }
    }

    private bool ShouldCache(HttpContext ctx)
    {
        if (!HttpMethods.IsGet(ctx.Request.Method)) return false;
        if (HasAuth(ctx)) return false;
        var path = ctx.Request.Path.Value ?? string.Empty;
        return _opts.Value.WhitelistPrefixes.Any(p => path.StartsWith(p, System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasAuth(HttpContext ctx)
    {
        if (ctx.Request.Headers.ContainsKey("Authorization")) return true;
        if (ctx.Request.Cookies.ContainsKey(BffSessionCookie.CookieName)) return true;
        return false;
    }

    private static string BuildKey(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? string.Empty;
        var sb = new StringBuilder(KeyPrefix);
        sb.Append(path);
        var sorted = ctx.Request.Query.OrderBy(q => q.Key, System.StringComparer.Ordinal).ToList();
        if (sorted.Count > 0)
        {
            sb.Append('?');
            for (var i = 0; i < sorted.Count; i++)
            {
                if (i > 0) sb.Append('&');
                sb.Append(sorted[i].Key).Append('=').Append(sorted[i].Value.ToString());
            }
        }
        var lang = ctx.Request.Headers.AcceptLanguage.FirstOrDefault() ?? "*";
        sb.Append("|lang=").Append(lang);
        return sb.ToString();
    }

    private sealed record Envelope(string ContentType, string Body);
}
