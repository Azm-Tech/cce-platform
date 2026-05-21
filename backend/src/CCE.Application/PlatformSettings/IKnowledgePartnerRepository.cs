using CCE.Domain.PlatformSettings;

namespace CCE.Application.PlatformSettings;

public interface IKnowledgePartnerRepository
{
    Task<KnowledgePartner?> FindAsync(System.Guid id, CancellationToken ct);
}
