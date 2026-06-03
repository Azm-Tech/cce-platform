using CCE.Application.Common;
using CCE.Application.Country.Dtos;
using MediatR;

namespace CCE.Application.Country.Queries.GetMyCountryProfile;

public sealed record GetMyCountryProfileQuery : IRequest<Response<CountryProfileDto>>;
