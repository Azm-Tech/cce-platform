using Microsoft.Extensions.Logging;

namespace CCE.Seeder;

/// <summary>
/// Orchestrates registered <see cref="ISeeder"/>s in <c>Order</c> ascending. Logs each step.
/// Failures bubble up — caller decides whether to abort startup.
/// </summary>
public sealed class SeedRunner
{
    private readonly IEnumerable<ISeeder> _seeders;
    private readonly ILogger<SeedRunner> _logger;

    public SeedRunner(IEnumerable<ISeeder> seeders, ILogger<SeedRunner> logger)
    {
        _seeders = seeders;
        _logger = logger;
    }

    public async Task RunAllAsync(bool includeDemo = false, CancellationToken ct = default)
    {
        var ordered = _seeders
            .Where(s => includeDemo || s.GetType().Name != "DemoDataSeeder")
            .OrderBy(s => s.Order)
            .ToList();

        _logger.LogInformation("Running {Count} seeders (demo={Demo}).", ordered.Count, includeDemo);

        foreach (var seeder in ordered)
        {
            var name = seeder.GetType().Name;
            _logger.LogInformation("→ {Seeder}", name);
            await seeder.SeedAsync(ct).ConfigureAwait(false);
        }

        _logger.LogInformation("Seeders complete.");
    }
}
