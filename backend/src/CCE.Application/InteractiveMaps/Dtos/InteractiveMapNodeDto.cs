namespace CCE.Application.InteractiveMaps.Dtos;

public sealed record InteractiveMapNodeDto(
    System.Guid Id,
    System.Guid InteractiveMapId,
    string NameAr,
    string NameEn,
    string IconKey,
    int? Category,
    string? CategoryNameAr,
    string? CategoryNameEn,
    string? TitleAr,
    string? TitleEn,
    string? DescriptionAr,
    string? DescriptionEn,
    System.Guid? ParentId,
    System.Guid TopicId,
    bool IsActive,
    System.Collections.Generic.IReadOnlyList<InteractiveMapTagDto> Tags);
