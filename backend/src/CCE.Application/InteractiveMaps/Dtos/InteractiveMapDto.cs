namespace CCE.Application.InteractiveMaps.Dtos;

public sealed record InteractiveMapDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string Slug,
    bool IsActive);
