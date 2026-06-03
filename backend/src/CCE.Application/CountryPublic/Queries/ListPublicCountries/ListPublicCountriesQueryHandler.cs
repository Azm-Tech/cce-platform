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
            .WhereIf(!string.IsNullOrWhiteSpace(request.Search), c =>
                c.NameAr.Contains(request.Search!) ||
                c.NameEn.Contains(request.Search!) ||
                c.IsoAlpha3.Contains(request.Search!) ||
                c.IsoAlpha2.Contains(request.Search!));

        // Left join with the latest KAPSARC snapshot — must happen before sort/project
        var query = from c in baseQuery
                    join s in _db.CountryKapsarcSnapshots
                        on c.LatestKapsarcSnapshotId equals s.Id into snapshotGroup
                    from s in snapshotGroup.DefaultIfEmpty()
                    select new { c, s };

        query = request.SortBy switch
        {
            PublicCountrySortBy.NameEn => request.SortOrder == SortOrder.Ascending
                ? query.OrderBy(x => x.c.NameEn)
                : query.OrderByDescending(x => x.c.NameEn),
            PublicCountrySortBy.PerformanceScore => request.SortOrder == SortOrder.Ascending
                ? query.OrderBy(x => x.s.PerformanceScore)
                : query.OrderByDescending(x => x.s.PerformanceScore),
            PublicCountrySortBy.TotalIndex => request.SortOrder == SortOrder.Ascending
                ? query.OrderBy(x => x.s.TotalIndex)
                : query.OrderByDescending(x => x.s.TotalIndex),
            _ => query.OrderByDescending(x => x.s.TotalIndex),
        };

        var page = await query
            .ToPagedResultAsync(
                x => new PublicCountryDto(
                    x.c.Id, x.c.IsoAlpha3, x.c.IsoAlpha2,
                    x.c.NameAr, x.c.NameEn, x.c.RegionAr, x.c.RegionEn, x.c.FlagUrl,
                    x.s.Classification,
                    x.s != null ? (decimal?)x.s.PerformanceScore : null,
                    x.s != null ? (decimal?)x.s.TotalIndex : null),
                request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return _messages.Ok(page, ApplicationErrors.General.SUCCESS_OPERATION);
    }
}
