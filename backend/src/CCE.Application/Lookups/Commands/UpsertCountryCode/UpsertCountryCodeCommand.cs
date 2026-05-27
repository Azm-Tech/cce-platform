using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Lookups.Commands.UpsertCountryCode;

public sealed record UpsertCountryCodeCommand(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string DialCode,
    bool IsActive) : IRequest<Response<CountryCodeDto>>;
