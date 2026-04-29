using CCE.Domain.InteractiveCity;

namespace CCE.Application.InteractiveCity;

public interface ICityScenarioService
{
    Task SaveAsync(CityScenario scenario, CancellationToken ct);
    Task<CityScenario?> FindAsync(System.Guid id, CancellationToken ct);
    Task UpdateAsync(CityScenario scenario, CancellationToken ct);
}
