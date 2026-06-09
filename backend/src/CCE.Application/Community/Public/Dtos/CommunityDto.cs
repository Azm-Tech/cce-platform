using CCE.Domain.Community;

namespace CCE.Application.Community.Public.Dtos;

public sealed record CommunityDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string DescriptionAr,
    string DescriptionEn,
    string Slug,
    CommunityVisibility Visibility,
    int MemberCount,
    string? PresentationJson);
