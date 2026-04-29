using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Reports;
using CCE.Application.Reports.Rows;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Reports;

public sealed class CountryProfilesReportService : ICountryProfilesReportService
{
    private readonly ICceDbContext _db;

    public CountryProfilesReportService(ICceDbContext db)
    {
        _db = db;
    }

    public async System.Collections.Generic.IAsyncEnumerable<CountryProfileReportRow> QueryAsync(
        System.DateTimeOffset? from,
        System.DateTimeOffset? to,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var query = _db.Countries
            .WithoutSoftDeleteFilter()
            .GroupJoin(
                _db.CountryProfiles,
                c => c.Id,
                p => p.CountryId,
                (c, profiles) => new { Country = c, Profile = profiles.FirstOrDefault() })
            .Select(x => new
            {
                x.Country.Id, x.Country.IsoAlpha3, x.Country.IsoAlpha2,
                x.Country.NameEn, x.Country.NameAr, x.Country.RegionEn,
                CountryIsActive = x.Country.IsActive,
                HasProfile = x.Profile != null,
                LastProfileUpdatedOn = x.Profile != null ? (System.DateTimeOffset?)x.Profile.LastUpdatedOn : null,
                LastProfileUpdatedById = x.Profile != null ? (System.Guid?)x.Profile.LastUpdatedById : null,
            });

        if (from.HasValue) query = query.Where(x => x.LastProfileUpdatedOn >= from);
        if (to.HasValue) query = query.Where(x => x.LastProfileUpdatedOn <= to);

        await foreach (var row in StreamAsAsyncEnumerable(query).WithCancellation(ct).ConfigureAwait(false))
        {
            yield return new CountryProfileReportRow
            {
                CountryId = row.Id,
                IsoAlpha3 = row.IsoAlpha3,
                IsoAlpha2 = row.IsoAlpha2,
                NameEn = row.NameEn,
                NameAr = row.NameAr,
                RegionEn = row.RegionEn,
                CountryIsActive = row.CountryIsActive,
                HasProfile = row.HasProfile,
                LastProfileUpdatedOn = row.LastProfileUpdatedOn,
                LastProfileUpdatedById = row.LastProfileUpdatedById,
            };
        }
    }

    private static async System.Collections.Generic.IAsyncEnumerable<T> StreamAsAsyncEnumerable<T>(IQueryable<T> query)
    {
        if (query is System.Collections.Generic.IAsyncEnumerable<T> asyncEnum)
        {
            await foreach (var item in asyncEnum)
            {
                yield return item;
            }
        }
        else
        {
            foreach (var item in query)
            {
                yield return item;
            }
        }
    }
}
