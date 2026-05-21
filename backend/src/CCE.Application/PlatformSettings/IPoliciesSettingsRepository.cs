using CCE.Domain.PlatformSettings;

namespace CCE.Application.PlatformSettings;

public interface IPoliciesSettingsRepository
{
    Task<PoliciesSettings?> GetAsync(CancellationToken ct);
}
