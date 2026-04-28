using CCE.Domain.Country;

namespace CCE.Application.Content;

public interface ICountryResourceRequestService
{
    Task<CountryResourceRequest?> FindIncludingDeletedAsync(System.Guid id, CancellationToken ct);
    Task UpdateAsync(CountryResourceRequest request, CancellationToken ct);
}
