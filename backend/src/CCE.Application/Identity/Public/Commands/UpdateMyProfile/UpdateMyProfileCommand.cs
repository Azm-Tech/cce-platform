using CCE.Application.Common;
using CCE.Application.Identity.Public.Dtos;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileCommand(
    System.Guid UserId,
    string FirstName,
    string LastName,
    string JobTitle,
    string OrganizationName,
    string LocalePreference,
    KnowledgeLevel KnowledgeLevel,
    string? AvatarUrl,
    System.Guid? CountryId,
    System.Guid? CountryCodeId) : IRequest<Response<UserProfileDto>>;
