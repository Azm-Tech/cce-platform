using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Country.Dtos;
using MediatR;

namespace CCE.Application.Country.Queries.ListCountries;

public sealed class ListCountriesQueryHandler
    : IRequestHandler<ListCountriesQuery, PagedResult<CountryDto>>
{
    private readonly ICceDbContext _db;

    public ListCountriesQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<CountryDto>> Handle(
        ListCountriesQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<CCE.Domain.Country.Country> query = _db.Countries;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(c =>
                c.NameAr.Contains(term) ||
                c.NameEn.Contains(term) ||
                c.IsoAlpha3.Contains(term) ||
                c.IsoAlpha2.Contains(term));
        }

        if (request.IsActive is { } isActive)
        {
            query = query.Where(c => c.IsActive == isActive);
        }

        query = query.OrderBy(c => c.NameEn);

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items.Select(MapToDto).ToList();
        return new PagedResult<CountryDto>(items, page.Page, page.PageSize, page.Total);
    }

    internal static CountryDto MapToDto(CCE.Domain.Country.Country c) => new(
        c.Id,
        c.IsoAlpha3,
        c.IsoAlpha2,
        c.NameAr,
        c.NameEn,
        c.RegionAr,
        c.RegionEn,
        c.FlagUrl,
        c.IsActive);
}
