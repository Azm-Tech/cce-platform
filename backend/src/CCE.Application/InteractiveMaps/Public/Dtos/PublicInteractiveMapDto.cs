using CCE.Domain.InteractiveMaps;

namespace CCE.Application.InteractiveMaps.Public.Dtos;

public sealed record PublicInteractiveMapDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string Slug,
    System.Collections.Generic.IReadOnlyList<PublicInteractiveMapNodeDto> Nodes)
{
    internal static PublicInteractiveMapDto FromEntity(InteractiveMap m, IReadOnlyList<PublicInteractiveMapNodeDto> nodes) => new(
        m.Id, m.NameAr, m.NameEn, m.DescriptionAr, m.DescriptionEn, m.Slug, nodes);
}
