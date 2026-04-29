namespace CCE.Application.Content.Public.Dtos;

public sealed record PublicResourceCategoryDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string Slug,
    System.Guid? ParentId,
    int OrderIndex);
