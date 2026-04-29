using CCE.Application.Country.Dtos;
using MediatR;

namespace CCE.Application.Country.Commands.UpsertCountryProfile;

public sealed record UpsertCountryProfileCommand(
    System.Guid CountryId,
    string DescriptionAr,
    string DescriptionEn,
    string KeyInitiativesAr,
    string KeyInitiativesEn,
    string? ContactInfoAr,
    string? ContactInfoEn,
    byte[] RowVersion) : IRequest<CountryProfileDto>;
