using CCE.Application.InteractiveCity;
using CCE.Domain.InteractiveCity;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.InteractiveCity;

public sealed class CityScenarioService : ICityScenarioService
{
    private readonly CceDbContext _db;

    public CityScenarioService(CceDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(CityScenario scenario, CancellationToken ct)
    {
        _db.CityScenarios.Add(scenario);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<CityScenario?> FindAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.CityScenarios
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(CityScenario scenario, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
