using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CCE.Api.Common.Auth;

public sealed class BffSessionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly BffSessionCookie _cookie;
    private readonly BffTokenRefresher _refresher;
    private readonly ILogger<BffSessionMiddleware> _logger;

    public BffSessionMiddleware(
        RequestDelegate next,
        BffSessionCookie cookie,
        BffTokenRefresher refresher,
        ILogger<BffSessionMiddleware> logger)
    {
        _next = next;
        _cookie = cookie;
        _refresher = refresher;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        var session = _cookie.TryRead(ctx);
        if (session is null)
        {
            await _next(ctx).ConfigureAwait(false);
            return;
        }
        if (session.ExpiresAt <= System.DateTimeOffset.UtcNow.AddSeconds(30))
        {
            var refreshed = await _refresher.TryRefreshAsync(session, ctx.RequestAborted).ConfigureAwait(false);
            if (refreshed is null)
            {
                _cookie.Clear(ctx);
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            _cookie.Write(ctx, refreshed);
            session = refreshed;
        }
        ctx.Request.Headers.Authorization = $"Bearer {session.AccessToken}";
        await _next(ctx).ConfigureAwait(false);
    }
}
