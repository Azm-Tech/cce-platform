using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.Reports.Dtos;
using MediatR;

namespace CCE.Application.Reports.Queries.GetCountryProfilesReport;

internal sealed class GetCountryProfilesReportQueryHandler(
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<GetCountryProfilesReportQuery, Response<PagedResult<CountryProfilesReportDto>>>
{
    public async Task<Response<PagedResult<CountryProfilesReportDto>>> Handle(
        GetCountryProfilesReportQuery q, CancellationToken ct)
    {
        var query = from c in _db.Countries.WithoutSoftDeleteFilter()
                    where c.IsCceCountry
                    join p in _db.CountryProfiles on c.Id equals p.CountryId into pJoin
                    from p in pJoin.DefaultIfEmpty()
                    join asset in _db.AssetFiles on p.NationallyDeterminedContributionAssetId equals asset.Id into assetJoin
                    from asset in assetJoin.DefaultIfEmpty()
                    join snap in _db.CountryKapsarcSnapshots on c.LatestKapsarcSnapshotId equals snap.Id into snapJoin
                    from snap in snapJoin.DefaultIfEmpty()
                    select new
                    {
                        c,
                        p,
                        NdcUrl = (string?)asset.Url,
                        snap
                    };

        if (q.From.HasValue)
            query = query.Where(x => x.p != null && (x.p.LastModifiedOn ?? x.p.CreatedOn) >= q.From.Value);
        if (q.To.HasValue)
            query = query.Where(x => x.p != null && (x.p.LastModifiedOn ?? x.p.CreatedOn) <= q.To.Value);

        query = query.OrderBy(x => x.c.NameEn);

        var paged = await query.ToPagedResultAsync(
            x => new CountryProfilesReportDto(
                x.p != null ? x.p.Id : x.c.Id,
                x.c.NameEn,
                x.p != null ? x.p.Population : null,
                x.p != null ? x.p.AreaSqKm : null,
                x.p != null ? x.p.GdpPerCapita : null,
                x.NdcUrl,
                x.snap != null ? x.snap.Classification : null,
                x.snap != null ? x.snap.PerformanceScore : null
            ),
            q.Page,
            q.PageSize,
            ct)
            .ConfigureAwait(false);

        return _msg.Ok(paged, MessageKeys.General.ITEMS_LISTED);
    }
}
