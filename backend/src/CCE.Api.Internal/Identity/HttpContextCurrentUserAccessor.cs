using System.Security.Claims;
using CCE.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CCE.Api.Internal.Identity;

/// <summary>
/// HttpContext-backed implementation of <see cref="ICurrentUserAccessor"/> for the Internal API host.
/// Reads the JWT <c>sub</c> claim (with <c>NameIdentifier</c> fallback) for both the actor string
/// and the user Guid. Returns <c>"system"</c> / <c>null</c> for unauthenticated requests.
/// </summary>
public sealed class HttpContextCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUserAccessor(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string GetActor()
    {
        var sub = ReadSubClaim();
        if (sub is null) return "system";
        return $"user:{sub}";
    }

    public System.Guid GetCorrelationId()
    {
        var ctx = _accessor.HttpContext;
        if (ctx is null) return System.Guid.Empty;
        var traceId = ctx.TraceIdentifier;
        return System.Guid.TryParse(traceId, out var g) ? g : System.Guid.Empty;
    }

    public System.Guid? GetUserId()
    {
        var sub = ReadSubClaim();
        return sub is not null && System.Guid.TryParse(sub, out var g) ? g : null;
    }

    private string? ReadSubClaim()
    {
        var user = _accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return null;
        return user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
