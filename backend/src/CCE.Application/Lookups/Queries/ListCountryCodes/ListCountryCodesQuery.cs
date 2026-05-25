using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Lookups.Queries.ListCountryCodes;

public sealed record ListCountryCodesQuery(
    string? Search = null,
    bool? IsActive = null) : IRequest<Response<IReadOnlyList<CountryCodeDto>>>;
