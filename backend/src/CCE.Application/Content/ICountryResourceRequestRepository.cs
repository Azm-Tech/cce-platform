using CCE.Domain.Country;

namespace CCE.Application.Content;

public interface ICountryContentRequestRepository
{
    Task<CountryContentRequest?> FindIncludingDeletedAsync(System.Guid id, CancellationToken ct);
    Task AddAsync(CountryContentRequest request, CancellationToken ct);
    Task UpdateAsync(CountryContentRequest request, CancellationToken ct);
}
