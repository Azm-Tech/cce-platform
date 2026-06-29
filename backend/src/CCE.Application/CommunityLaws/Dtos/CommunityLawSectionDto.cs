namespace CCE.Application.CommunityLaws.Dtos;

public sealed record CommunityLawSectionDto(
    Guid Id,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn,
    int OrderIndex);
