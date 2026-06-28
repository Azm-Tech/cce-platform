using CCE.Application.Common;
using CCE.Application.Content;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListPages;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.CreatePage;

public sealed class CreatePageCommandHandler : IRequestHandler<CreatePageCommand, Response<PageDto>>
{
    private readonly IPageRepository _service;
    private readonly MessageFactory _msg;

    public CreatePageCommandHandler(IPageRepository service, MessageFactory msg)
    {
        _service = service;
        _msg = msg;
    }

    public async Task<Response<PageDto>> Handle(CreatePageCommand request, CancellationToken cancellationToken)
    {
        var page = Page.Create(
            request.Slug, request.PageType,
            request.TitleAr, request.TitleEn,
            request.ContentAr, request.ContentEn);
        await _service.SaveAsync(page, cancellationToken).ConfigureAwait(false);
        return _msg.Ok(ListPagesQueryHandler.MapToDto(page), MessageKeys.Content.CONTENT_CREATED);
    }
}
