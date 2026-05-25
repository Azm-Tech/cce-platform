using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListPages;
using MediatR;

namespace CCE.Application.Content.Commands.UpdatePage;

public sealed class UpdatePageCommandHandler : IRequestHandler<UpdatePageCommand, PageDto?>
{
    private readonly IPageRepository _service;

    public UpdatePageCommandHandler(IPageRepository service)
    {
        _service = service;
    }

    public async Task<PageDto?> Handle(UpdatePageCommand request, CancellationToken cancellationToken)
    {
        var page = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (page is null)
        {
            return null;
        }

        page.UpdateContent(
            request.TitleAr,
            request.TitleEn,
            request.ContentAr,
            request.ContentEn);

        await _service.UpdateAsync(page, request.RowVersion, cancellationToken).ConfigureAwait(false);

        return ListPagesQueryHandler.MapToDto(page);
    }
}
