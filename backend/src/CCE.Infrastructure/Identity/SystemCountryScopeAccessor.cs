using CCE.Application.Common.CountryScope;

namespace CCE.Infrastructure.Identity;

/// <summary>
/// Default registration: returns null (no scope). API hosts override with HttpContext-based impl.
/// </summary>
public sealed class SystemCountryScopeAccessor : ICountryScopeAccessor
{
    public Task<IReadOnlyList<System.Guid>?> GetAuthorizedCountryIdsAsync(System.Threading.CancellationToken ct)
        => Task.FromResult<IReadOnlyList<System.Guid>?>(null);
}
