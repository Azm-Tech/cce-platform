using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Country.Dtos;
using CCE.Application.Country.Queries.ListCountries;
using MediatR;

namespace CCE.Application.Country.Queries.GetCountryById;

public sealed class GetCountryByIdQueryHandler : IRequestHandler<GetCountryByIdQuery, CountryDto?>
{
    private readonly ICceDbContext _db;

    public GetCountryByIdQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<CountryDto?> Handle(GetCountryByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Countries
            .Where(c => c.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var country = list.SingleOrDefault();
        return country is null ? null : ListCountriesQueryHandler.MapToDto(country);
    }
}
