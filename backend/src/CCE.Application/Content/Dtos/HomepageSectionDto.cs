using CCE.Domain.Content;

namespace CCE.Application.Content.Dtos;

public sealed record HomepageSectionDto(
    System.Guid Id,
    HomepageSectionType SectionType,
    int OrderIndex,
    string ContentAr,
    string ContentEn,
    bool IsActive);
