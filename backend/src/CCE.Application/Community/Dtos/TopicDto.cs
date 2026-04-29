namespace CCE.Application.Community.Dtos;

public sealed record TopicDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string DescriptionAr,
    string DescriptionEn,
    string Slug,
    System.Guid? ParentId,
    string? IconUrl,
    int OrderIndex,
    bool IsActive);
