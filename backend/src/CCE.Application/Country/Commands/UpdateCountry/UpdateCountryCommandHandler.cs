using CCE.Application.Common;
using CCE.Application.Country.Dtos;
using CCE.Application.Country.Queries.ListCountries;
using CCE.Application.Messages;

using MediatR;

namespace CCE.Application.Country.Commands.UpdateCountry;

public sealed class UpdateCountryCommandHandler : IRequestHandler<UpdateCountryCommand, Response<CountryDto>>
{
    private readonly ICountryAdminService _service;
    private readonly MessageFactory _msg;

    public UpdateCountryCommandHandler(ICountryAdminService service, MessageFactory msg)
    {
        _service = service;
        _msg = msg;
    }

    public async Task<Response<CountryDto>> Handle(UpdateCountryCommand request, CancellationToken cancellationToken)
    {
        var country = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (country is null)
        {
            return _msg.NotFound<CountryDto>(MessageKeys.Country.COUNTRY_NOT_FOUND);
        }

        country.UpdateNames(request.NameAr, request.NameEn, request.RegionAr, request.RegionEn);

        if (request.IsActive)
            country.Activate();
        else
            country.Deactivate();

        await _service.UpdateAsync(country, cancellationToken).ConfigureAwait(false);

        return _msg.Ok(ListCountriesQueryHandler.MapToDto(country), MessageKeys.General.SUCCESS_UPDATED);
    }
}
