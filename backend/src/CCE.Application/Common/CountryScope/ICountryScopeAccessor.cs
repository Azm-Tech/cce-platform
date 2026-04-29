namespace CCE.Application.Common.CountryScope;

/// <summary>
/// Resolves the set of country IDs the current request is authorized to query.
/// <c>null</c> means no scope (admin / content-manager / anonymous-on-public-route).
/// A non-null list means StateRepresentative — restrict country-scoped reads to those ids.
/// An empty list means authenticated-but-no-state-rep — sees nothing in country-scoped queries.
/// </summary>
public interface ICountryScopeAccessor
{
    Task<IReadOnlyList<System.Guid>?> GetAuthorizedCountryIdsAsync(System.Threading.CancellationToken ct);
}
