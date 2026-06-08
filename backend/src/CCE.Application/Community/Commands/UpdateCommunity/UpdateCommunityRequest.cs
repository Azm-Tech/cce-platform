namespace CCE.Application.Community.Commands.UpdateCommunity;

public sealed record UpdateCommunityRequest(
    string NameAr,
    string NameEn,
    string DescriptionAr,
    string DescriptionEn,
    string? PresentationJson);
