using CCE.Application.InterestManagement.Dtos;

namespace CCE.Application.InterestManagement.Dtos;

public sealed record InterestCategoryInfoDto(
    string Category,
    string TitleAr,
    string TitleEn,
    string Type,
    IReadOnlyList<InterestTopicDto> Options);