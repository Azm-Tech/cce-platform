using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Kapsarc.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Country;
using MediatR;

namespace CCE.Application.Kapsarc.Queries.GetLatestKapsarcSnapshot;

public sealed class GetLatestKapsarcSnapshotQueryHandler
    : IRequestHandler<GetLatestKapsarcSnapshotQuery, Response<KapsarcSnapshotDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetLatestKapsarcSnapshotQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<KapsarcSnapshotDto>> Handle(
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

        if (latest is null)
            return _msg.NotFound<KapsarcSnapshotDto>(MessageKeys.Country.KAPSARC_DATA_UNAVAILABLE);

        return _msg.Ok(MapToDto(latest), MessageKeys.General.SUCCESS_OPERATION);
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
