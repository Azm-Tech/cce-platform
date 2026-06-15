using CCE.Domain.InteractiveMaps;

namespace CCE.Application.InteractiveMaps.Public.Dtos;

public sealed record PublicInteractiveMapNodeDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string IconKey,
    int? Category,
    string? CategoryNameAr,
    string? CategoryNameEn,
    int Level,
    System.Guid? ParentId,
    System.Guid? TopicId,
    string? TopicSlug)
{
    internal static PublicInteractiveMapNodeDto FromEntity(InteractiveMapNode n) => new(
        n.Id, n.NameAr, n.NameEn, n.IconKey,
        n.Category, n.CategoryNameAr, n.CategoryNameEn,
        n.Level, n.ParentId, n.TopicId, n.TopicSlug);
}
