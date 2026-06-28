using CCE.Domain.Community;

namespace CCE.Application.Community.Commands.CreateCommunity;

public sealed record CreateCommunityRequest(
    string NameAr,
    string NameEn,
    string DescriptionAr,
    string DescriptionEn,
    string Slug,
    CommunityVisibility Visibility,
    string? PresentationJson);
