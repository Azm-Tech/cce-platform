using CCE.Domain.Content;

namespace CCE.Application.Content.Public.Dtos;

public sealed record PublicPageDto(
    System.Guid Id,
    string Slug,
    PageType PageType,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn);
