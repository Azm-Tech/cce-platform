using CCE.Application.Media;
using CCE.Domain.Media;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Media;

public sealed class MediaFileRepository : IMediaFileRepository
{
    private readonly CceDbContext _db;

    public MediaFileRepository(CceDbContext db) => _db = db;

    public async Task<MediaFile?> FindAsync(System.Guid id, CancellationToken ct)
        => await _db.MediaFiles.FirstOrDefaultAsync(m => m.Id == id, ct).ConfigureAwait(false);
}
