using CCE.Application.Identity.Public.Dtos;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileCommand(
    System.Guid UserId,
    string LocalePreference,
    KnowledgeLevel KnowledgeLevel,
    IReadOnlyList<string> Interests,
    string? AvatarUrl,
    System.Guid? CountryId) : IRequest<UserProfileDto?>;
