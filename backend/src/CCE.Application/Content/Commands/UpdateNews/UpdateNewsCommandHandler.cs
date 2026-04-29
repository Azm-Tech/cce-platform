using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListNews;
using MediatR;

namespace CCE.Application.Content.Commands.UpdateNews;

public sealed class UpdateNewsCommandHandler : IRequestHandler<UpdateNewsCommand, NewsDto?>
{
    private readonly INewsService _service;

    public UpdateNewsCommandHandler(INewsService service)
    {
        _service = service;
    }

    public async Task<NewsDto?> Handle(UpdateNewsCommand request, CancellationToken cancellationToken)
    {
        var news = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (news is null)
        {
            return null;
        }

        news.UpdateContent(
            request.TitleAr,
            request.TitleEn,
            request.ContentAr,
            request.ContentEn,
            request.Slug,
            request.FeaturedImageUrl);

        await _service.UpdateAsync(news, request.RowVersion, cancellationToken).ConfigureAwait(false);

        return ListNewsQueryHandler.MapToDto(news);
    }
}
