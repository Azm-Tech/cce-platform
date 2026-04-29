using CCE.Application.Content.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.CreatePage;

public sealed record CreatePageCommand(
    string Slug,
    PageType PageType,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn) : IRequest<PageDto>;
