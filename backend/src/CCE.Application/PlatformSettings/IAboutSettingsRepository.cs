using CCE.Domain.PlatformSettings;

namespace CCE.Application.PlatformSettings;

/// <summary>Repository for the single-row AboutSettings aggregate (with children).</summary>
public interface IAboutSettingsRepository
{
    Task<AboutSettings?> GetAsync(CancellationToken ct);
}
