using CCE.Domain.Media;

namespace CCE.Application.Media;

public interface IMediaFileRepository
{
    Task<MediaFile?> FindAsync(System.Guid id, CancellationToken ct);
}
