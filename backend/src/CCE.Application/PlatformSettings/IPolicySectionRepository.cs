using CCE.Domain.PlatformSettings;

namespace CCE.Application.PlatformSettings;

public interface IPolicySectionRepository
{
    Task<PolicySection?> FindAsync(System.Guid id, CancellationToken ct);
}
