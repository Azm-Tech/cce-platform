namespace CCE.Application.Country;

public interface ICountryAdminService
{
    Task<CCE.Domain.Country.Country?> FindAsync(System.Guid id, CancellationToken ct);
    Task UpdateAsync(CCE.Domain.Country.Country country, CancellationToken ct);
}
