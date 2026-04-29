using CCE.Domain.Content;

namespace CCE.Application.Content.Public.Dtos;

public sealed record PublicHomepageSectionDto(
    System.Guid Id,
    HomepageSectionType SectionType,
    int OrderIndex,
    string ContentAr,
    string ContentEn);
