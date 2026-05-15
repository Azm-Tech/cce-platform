using CCE.Application.Content;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListNews;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Content.Commands.PublishNews;

public sealed class PublishNewsCommandHandler : IRequestHandler<PublishNewsCommand, NewsDto?>
{
    private readonly INewsRepository _service;
    private readonly ISystemClock _clock;

    public PublishNewsCommandHandler(INewsRepository service, ISystemClock clock)
    {
        _service = service;
        _clock = clock;
    }

    public async Task<NewsDto?> Handle(PublishNewsCommand request, CancellationToken cancellationToken)
    {
        var news = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (news is null)
        {
            return null;
        }

        var expectedRowVersion = news.RowVersion;
        news.Publish(_clock);
        await _service.UpdateAsync(news, expectedRowVersion, cancellationToken).ConfigureAwait(false);

        return ListNewsQueryHandler.MapToDto(news);
    }
}
