using CCE.Domain.PlatformSettings;

namespace CCE.Application.PlatformSettings;

public interface IAboutSettingsRepository
{
    Task<AboutSettings?> GetAsync(CancellationToken ct);
}
