using CCE.Domain.PlatformSettings;

namespace CCE.Application.PlatformSettings;

/// <summary>Repository for the standalone FAQ entity.</summary>
public interface IFaqRepository
{
    Task<Faq?> GetByIdAsync(System.Guid id, CancellationToken ct);
    void Add(Faq faq);
    void Delete(Faq faq);
}
