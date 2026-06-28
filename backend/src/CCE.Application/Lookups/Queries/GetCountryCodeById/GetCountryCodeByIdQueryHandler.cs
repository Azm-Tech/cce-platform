using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Lookups.Queries.GetCountryCodeById;

public sealed class GetCountryCodeByIdQueryHandler
    : IRequestHandler<GetCountryCodeByIdQuery, Response<CountryCodeDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetCountryCodeByIdQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<CountryCodeDto>> Handle(
        GetCountryCodeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _db.Countries
            .Where(c => c.Id == request.Id && c.DialCode != null)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var entity = list.SingleOrDefault();
        return entity is null
            ? _msg.NotFound<CountryCodeDto>(MessageKeys.Lookups.COUNTRY_CODE_NOT_FOUND)
            : _msg.Ok(CCE.Application.Lookups.Queries.ListCountryCodes.ListCountryCodesQueryHandler.MapToDto(entity), MessageKeys.General.ITEMS_LISTED);
    }
}
