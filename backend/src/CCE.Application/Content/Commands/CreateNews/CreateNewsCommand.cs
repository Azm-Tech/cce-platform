using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.CreateNews;

public sealed record CreateNewsCommand(
    string TitleAr, string TitleEn,
    string ContentAr, string ContentEn,
    string Slug,
    string? FeaturedImageUrl) : IRequest<NewsDto>;
