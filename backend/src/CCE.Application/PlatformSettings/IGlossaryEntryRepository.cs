using CCE.Domain.PlatformSettings;

namespace CCE.Application.PlatformSettings;

public interface IGlossaryEntryRepository
{
    Task<GlossaryEntry?> FindAsync(System.Guid id, CancellationToken ct);
}
