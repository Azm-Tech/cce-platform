using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.CountryPublic.Dtos;
using MediatR;

namespace CCE.Application.CountryPublic.Queries.ListPublicCountries;

public sealed class ListPublicCountriesQueryHandler
    : IRequestHandler<ListPublicCountriesQuery, IReadOnlyList<PublicCountryDto>>
{
    private readonly ICceDbContext _db;

    public ListPublicCountriesQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PublicCountryDto>> Handle(
        ListPublicCountriesQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<CCE.Domain.Country.Country> query = _db.Countries
            .Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(c =>
                c.NameAr.Contains(term) ||
                c.NameEn.Contains(term) ||
                c.IsoAlpha3.Contains(term) ||
                c.IsoAlpha2.Contains(term));
        }

        query = query.OrderBy(c => c.NameEn);

        var items = await query.ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        return items.Select(MapToDto).ToList();
    }

    internal static PublicCountryDto MapToDto(CCE.Domain.Country.Country c) => new(
        c.Id,
        c.IsoAlpha3,
        c.IsoAlpha2,
        c.NameAr,
        c.NameEn,
        c.RegionAr,
        c.RegionEn,
        c.FlagUrl);
}
