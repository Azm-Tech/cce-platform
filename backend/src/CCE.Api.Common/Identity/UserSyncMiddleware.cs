using System.Security.Claims;
using CCE.Application.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CCE.Api.Common.Identity;

/// <summary>
/// On every authenticated request, ensures the current user has a row in <c>users</c>.
/// Runs AFTER <c>UseAuthentication</c> + <c>UseAuthorization</c>, BEFORE endpoint dispatch.
/// Idempotent — uses <see cref="IMemoryCache"/> keyed by JWT <c>sub</c> for 5 min so repeat
/// requests skip the DB.
/// </summary>
public sealed class UserSyncMiddleware
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly RequestDelegate _next;
    private readonly ILogger<UserSyncMiddleware> _logger;

    public UserSyncMiddleware(RequestDelegate next, ILogger<UserSyncMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IMemoryCache cache,
        IUserSyncService syncService)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var subClaim = context.User.FindFirstValue("sub")
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(subClaim, out var userId))
        {
            _logger.LogWarning("Authenticated request has no parseable sub claim; skipping user sync.");
            await _next(context).ConfigureAwait(false);
            return;
        }

        var cacheKey = $"user-synced:{userId:N}";
        if (cache.TryGetValue(cacheKey, out _))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var email = context.User.FindFirstValue("email") ?? $"{userId:N}@unknown.local";
        var preferredUsername = context.User.FindFirstValue("preferred_username") ?? email;
        var groups = context.User.FindAll("groups").Select(c => c.Value)
            .Concat(context.User.FindAll(ClaimTypes.Role).Select(c => c.Value))
            .ToList();

        await syncService.EnsureUserExistsAsync(userId, email, preferredUsername, groups, context.RequestAborted)
            .ConfigureAwait(false);

        cache.Set(cacheKey, true, CacheTtl);
        await _next(context).ConfigureAwait(false);
    }
}
