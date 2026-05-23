using CCE.Domain.PlatformSettings;

namespace CCE.Application.PlatformSettings;

/// <summary>Repository for the single-row HomepageSettings aggregate (with children).</summary>
public interface IHomepageSettingsRepository
{
    Task<HomepageSettings?> GetAsync(CancellationToken ct);
}
