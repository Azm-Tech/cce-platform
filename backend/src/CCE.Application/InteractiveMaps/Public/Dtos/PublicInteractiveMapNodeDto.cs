using CCE.Application.InteractiveMaps.Dtos;
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
    System.Guid TopicId,
    System.Collections.Generic.IReadOnlyList<TagDto> Tags)
{
    internal static PublicInteractiveMapNodeDto FromEntity(InteractiveMapNode n) => new(
        n.Id, n.NameAr, n.NameEn, n.IconKey,
        n.Category, n.CategoryNameAr, n.CategoryNameEn,
        n.Level, n.ParentId, n.TopicId,
        n.Tags.Select(t => new TagDto(t.Id, t.NameAr, t.NameEn)).ToList());
}
