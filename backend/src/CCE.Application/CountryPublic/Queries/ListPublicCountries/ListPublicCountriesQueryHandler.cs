using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.CountryPublic.Dtos;
using CCE.Application.Errors;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Country;
using MediatR;

namespace CCE.Application.CountryPublic.Queries.ListPublicCountries;

public sealed class ListPublicCountriesQueryHandler
    : IRequestHandler<ListPublicCountriesQuery, Response<PagedResult<PublicCountryDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public ListPublicCountriesQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<PagedResult<PublicCountryDto>>> Handle(
        ListPublicCountriesQuery request,
        CancellationToken cancellationToken)
    {
        var baseQuery = _db.Countries
            .Where(c => c.IsActive)
            .WhereIf(request.IsCceCountry.HasValue, c => c.IsCceCountry == request.IsCceCountry!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(request.Search), c =>
                c.NameAr.Contains(request.Search!) ||
                c.NameEn.Contains(request.Search!) ||
                (c.IsoAlpha3 != null && c.IsoAlpha3.Contains(request.Search!)) ||
                (c.IsoAlpha2 != null && c.IsoAlpha2.Contains(request.Search!)) ||
                (c.DialCode != null && c.DialCode.Contains(request.Search!)));

        // KAPSARC join and score-based sorting only apply to CCE countries.
        if (request.IsCceCountry == true)
        {
            var cceQuery = from c in baseQuery
                           join s in _db.CountryKapsarcSnapshots
                               on c.LatestKapsarcSnapshotId equals s.Id into snapshotGroup
                           from s in snapshotGroup.DefaultIfEmpty()
                           select new { c, s };

            cceQuery = request.SortBy switch
            {
                PublicCountrySortBy.PerformanceScore => request.SortOrder == SortOrder.Ascending
                    ? cceQuery.OrderBy(x => x.s.PerformanceScore)
                    : cceQuery.OrderByDescending(x => x.s.PerformanceScore),
                PublicCountrySortBy.TotalIndex => request.SortOrder == SortOrder.Ascending
                    ? cceQuery.OrderBy(x => x.s.TotalIndex)
                    : cceQuery.OrderByDescending(x => x.s.TotalIndex),
                _ => request.SortOrder == SortOrder.Ascending
                    ? cceQuery.OrderBy(x => x.c.NameEn)
                    : cceQuery.OrderByDescending(x => x.c.NameEn),
            };

            var ccePage = await cceQuery
                .ToPagedResultAsync(
                    x => new PublicCountryDto(
                        x.c.Id, x.c.IsoAlpha3, x.c.IsoAlpha2,
                        x.c.NameAr, x.c.NameEn, x.c.RegionAr, x.c.RegionEn, x.c.FlagUrl,
                        x.c.DialCode, x.c.IsCceCountry,
                        x.s != null ? x.s.Classification : null,
                        x.s != null ? (decimal?)x.s.PerformanceScore : null,
                        x.s != null ? (decimal?)x.s.TotalIndex : null),
                    request.Page, request.PageSize, cancellationToken)
                .ConfigureAwait(false);

            return _messages.Ok(ccePage, ApplicationErrors.General.SUCCESS_OPERATION);
        }

        // Simple flat list — no KAPSARC join needed.
        var sorted = request.SortOrder == SortOrder.Ascending
            ? baseQuery.OrderBy(c => c.NameEn)
            : baseQuery.OrderByDescending(c => c.NameEn);

        var page = await sorted
            .ToPagedResultAsync(
                c => new PublicCountryDto(
                    c.Id, c.IsoAlpha3, c.IsoAlpha2,
                    c.NameAr, c.NameEn, c.RegionAr, c.RegionEn, c.FlagUrl,
                    c.DialCode, c.IsCceCountry,
                    null, null, null),
                request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return _messages.Ok(page, ApplicationErrors.General.SUCCESS_OPERATION);
    }
}
