using System.Security.Claims;
using CCE.Application.Common.CountryScope;
using CCE.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CCE.Api.Common.Identity;

/// <summary>
/// Reads the JWT sub claim and looks up active state-representative assignments.
/// SuperAdmin / ContentManager / anonymous → null (no scope).
/// StateRepresentative → list of CountryId from active assignments.
/// Other authenticated roles → empty list (sees nothing in country-scoped queries).
/// </summary>
public sealed class HttpContextCountryScopeAccessor : ICountryScopeAccessor
{
    private static readonly string[] BypassRoles = new[] { "SuperAdmin", "ContentManager" };

    private readonly IHttpContextAccessor _accessor;
    private readonly ICceDbContext _db;

    public HttpContextCountryScopeAccessor(IHttpContextAccessor accessor, ICceDbContext db)
    {
        _accessor = accessor;
        _db = db;
    }

    public async Task<IReadOnlyList<System.Guid>?> GetAuthorizedCountryIdsAsync(System.Threading.CancellationToken ct)
    {
        var user = _accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }
        var groups = user.FindAll("groups").Select(c => c.Value)
            .ToHashSet(System.StringComparer.OrdinalIgnoreCase);
        if (BypassRoles.Any(r => groups.Contains(r)))
        {
            return null;
        }
        if (!groups.Contains("StateRepresentative"))
        {
            return System.Array.Empty<System.Guid>();
        }

        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!System.Guid.TryParse(sub, out var userId))
        {
            return System.Array.Empty<System.Guid>();
        }

        var ids = await _db.StateRepresentativeAssignments
            .Where(a => a.UserId == userId)
            .Select(a => a.CountryId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return ids;
    }
}
