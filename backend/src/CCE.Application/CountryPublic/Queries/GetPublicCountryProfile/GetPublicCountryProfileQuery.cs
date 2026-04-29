using CCE.Application.CountryPublic.Dtos;
using MediatR;

namespace CCE.Application.CountryPublic.Queries.GetPublicCountryProfile;

public sealed record GetPublicCountryProfileQuery(System.Guid CountryId) : IRequest<PublicCountryProfileDto?>;
