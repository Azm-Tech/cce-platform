using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Lookups.Queries.GetCountryCodeById;

public sealed record GetCountryCodeByIdQuery(System.Guid Id) : IRequest<Response<CountryCodeDto>>;
