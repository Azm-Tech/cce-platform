using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Country.Dtos;
using CCE.Application.Country.Queries.ListCountries;
using CCE.Application.Messages;

using MediatR;

namespace CCE.Application.Country.Queries.GetCountryById;

public sealed class GetCountryByIdQueryHandler : IRequestHandler<GetCountryByIdQuery, Response<CountryDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetCountryByIdQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<CountryDto>> Handle(GetCountryByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Countries
            .Where(c => c.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var country = list.SingleOrDefault();
        return country is null
            ? _msg.NotFound<CountryDto>(MessageKeys.Country.COUNTRY_NOT_FOUND)
            : _msg.Ok(ListCountriesQueryHandler.MapToDto(country), MessageKeys.General.SUCCESS_OPERATION);
    }
}
