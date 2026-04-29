namespace CCE.Application.Country;

public interface ICountryProfileService
{
    Task<CCE.Domain.Country.CountryProfile?> FindByCountryIdAsync(System.Guid countryId, CancellationToken ct);
    Task SaveAsync(CCE.Domain.Country.CountryProfile profile, CancellationToken ct);
    Task UpdateAsync(CCE.Domain.Country.CountryProfile profile, byte[] expectedRowVersion, CancellationToken ct);
}
