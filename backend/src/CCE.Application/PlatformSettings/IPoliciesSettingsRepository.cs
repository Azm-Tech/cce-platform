using CCE.Domain.PlatformSettings;

namespace CCE.Application.PlatformSettings;

/// <summary>Repository for the single-row PoliciesSettings aggregate (with children).</summary>
public interface IPoliciesSettingsRepository
{
    Task<PoliciesSettings?> GetAsync(CancellationToken ct);
}
