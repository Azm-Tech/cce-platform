using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Kapsarc.Dtos;
using CCE.Domain.Country;
using MediatR;

namespace CCE.Application.Kapsarc.Queries.GetLatestKapsarcSnapshot;

public sealed class GetLatestKapsarcSnapshotQueryHandler
    : IRequestHandler<GetLatestKapsarcSnapshotQuery, KapsarcSnapshotDto?>
{
    private readonly ICceDbContext _db;

    public GetLatestKapsarcSnapshotQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<KapsarcSnapshotDto?> Handle(
        GetLatestKapsarcSnapshotQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _db.CountryKapsarcSnapshots
            .Where(s => s.CountryId == request.CountryId)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var latest = rows
            .OrderByDescending(s => s.SnapshotTakenOn)
            .FirstOrDefault();

        return latest is null ? null : MapToDto(latest);
    }

    internal static KapsarcSnapshotDto MapToDto(CountryKapsarcSnapshot s) => new(
        s.Id,
        s.CountryId,
        s.Classification,
        s.PerformanceScore,
        s.TotalIndex,
        s.SnapshotTakenOn,
        s.SourceVersion);
}
