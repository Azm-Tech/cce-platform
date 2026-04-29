namespace CCE.Application.Community.Public.Dtos;

public sealed record PublicTopicDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string DescriptionAr,
    string DescriptionEn,
    string Slug,
    System.Guid? ParentId,
    string? IconUrl,
    int OrderIndex);
