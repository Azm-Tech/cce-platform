using CCE.Application.Content;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListPages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.CreatePage;

public sealed class CreatePageCommandHandler : IRequestHandler<CreatePageCommand, PageDto>
{
    private readonly IPageService _service;

    public CreatePageCommandHandler(IPageService service)
    {
        _service = service;
    }

    public async Task<PageDto> Handle(CreatePageCommand request, CancellationToken cancellationToken)
    {
        var page = Page.Create(
            request.Slug, request.PageType,
            request.TitleAr, request.TitleEn,
            request.ContentAr, request.ContentEn);
        await _service.SaveAsync(page, cancellationToken).ConfigureAwait(false);
        return ListPagesQueryHandler.MapToDto(page);
    }
}
