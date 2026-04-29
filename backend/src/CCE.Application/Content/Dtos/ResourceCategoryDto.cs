namespace CCE.Application.Content.Dtos;

public sealed record ResourceCategoryDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string Slug,
    System.Guid? ParentId,
    int OrderIndex,
    bool IsActive);
