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
    int Level,
    System.Guid? ParentId,
    System.Guid TopicId,
    bool IsActive,
    System.Collections.Generic.IReadOnlyList<TagDto> Tags);
