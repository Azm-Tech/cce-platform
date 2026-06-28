using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListPages;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Content.Commands.UpdatePage;

public sealed class UpdatePageCommandHandler : IRequestHandler<UpdatePageCommand, Response<PageDto>>
{
    private readonly IPageRepository _service;
    private readonly MessageFactory _msg;

    public UpdatePageCommandHandler(IPageRepository service, MessageFactory msg)
    {
        _service = service;
        _msg = msg;
    }

    public async Task<Response<PageDto>> Handle(UpdatePageCommand request, CancellationToken cancellationToken)
    {
        var page = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (page is null)
        {
            return _msg.NotFound<PageDto>(MessageKeys.Content.PAGE_NOT_FOUND);
        }

        page.UpdateContent(
            request.TitleAr,
            request.TitleEn,
            request.ContentAr,
            request.ContentEn);

        await _service.UpdateAsync(page, request.RowVersion, cancellationToken).ConfigureAwait(false);

        return _msg.Ok(ListPagesQueryHandler.MapToDto(page), MessageKeys.Content.CONTENT_UPDATED);
    }
}
