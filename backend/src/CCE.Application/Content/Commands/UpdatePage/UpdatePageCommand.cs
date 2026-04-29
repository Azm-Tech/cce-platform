using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.UpdatePage;

public sealed record UpdatePageCommand(
    System.Guid Id,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn,
    byte[] RowVersion) : IRequest<PageDto?>;
