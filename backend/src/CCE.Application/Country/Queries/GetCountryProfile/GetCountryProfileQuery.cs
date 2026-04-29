using CCE.Application.Country.Dtos;
using MediatR;

namespace CCE.Application.Country.Queries.GetCountryProfile;

public sealed record GetCountryProfileQuery(System.Guid CountryId) : IRequest<CountryProfileDto?>;
