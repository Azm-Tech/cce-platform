using CCE.Application.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.CreateCommunity;

public sealed record CreateCommunityCommand(
    string NameAr,
    string NameEn,
    string DescriptionAr,
    string DescriptionEn,
    string Slug,
    CommunityVisibility Visibility,
    string? PresentationJson) : IRequest<Response<Guid>>;
