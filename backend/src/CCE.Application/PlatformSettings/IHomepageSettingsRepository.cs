using CCE.Domain.PlatformSettings;

namespace CCE.Application.PlatformSettings;

public interface IHomepageSettingsRepository
{
    Task<HomepageSettings?> GetAsync(CancellationToken ct);
}
