using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.GetCountryContentRequest;

public sealed record GetCountryContentRequestQuery(System.Guid Id)
    : IRequest<Response<CountryContentRequestDto>>;
