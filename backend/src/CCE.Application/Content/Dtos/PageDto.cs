using CCE.Domain.Content;

namespace CCE.Application.Content.Dtos;

public sealed record PageDto(
    System.Guid Id,
    string Slug,
    PageType PageType,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn,
    string RowVersion);
