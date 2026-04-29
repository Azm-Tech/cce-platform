using CCE.Application.Country.Dtos;
using MediatR;

namespace CCE.Application.Country.Commands.UpdateCountry;

public sealed record UpdateCountryCommand(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string RegionAr,
    string RegionEn,
    bool IsActive) : IRequest<CountryDto?>;
