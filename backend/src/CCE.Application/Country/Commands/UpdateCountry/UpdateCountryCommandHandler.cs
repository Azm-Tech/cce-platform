using CCE.Application.Country.Dtos;
using CCE.Application.Country.Queries.ListCountries;
using MediatR;

namespace CCE.Application.Country.Commands.UpdateCountry;

public sealed class UpdateCountryCommandHandler : IRequestHandler<UpdateCountryCommand, CountryDto?>
{
    private readonly ICountryAdminService _service;

    public UpdateCountryCommandHandler(ICountryAdminService service)
    {
        _service = service;
    }

    public async Task<CountryDto?> Handle(UpdateCountryCommand request, CancellationToken cancellationToken)
    {
        var country = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (country is null)
        {
            return null;
        }

        country.UpdateNames(request.NameAr, request.NameEn, request.RegionAr, request.RegionEn);

        if (request.IsActive)
            country.Activate();
        else
            country.Deactivate();

        await _service.UpdateAsync(country, cancellationToken).ConfigureAwait(false);

        return ListCountriesQueryHandler.MapToDto(country);
    }
}
