using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.UpdateNews;

public sealed record UpdateNewsCommand(
    System.Guid Id,
    string TitleAr, string TitleEn,
    string ContentAr, string ContentEn,
    string Slug,
    string? FeaturedImageUrl,
    byte[] RowVersion) : IRequest<NewsDto?>;
