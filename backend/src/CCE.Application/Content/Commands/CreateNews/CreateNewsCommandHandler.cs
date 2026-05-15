using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListNews;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.CreateNews;

public sealed class CreateNewsCommandHandler : IRequestHandler<CreateNewsCommand, NewsDto>
{
    private readonly INewsRepository _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public CreateNewsCommandHandler(
        INewsRepository service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<NewsDto> Handle(CreateNewsCommand request, CancellationToken cancellationToken)
    {
        var authorId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot create a news article from a request without a user identity.");

        var news = News.Draft(
            request.TitleAr,
            request.TitleEn,
            request.ContentAr,
            request.ContentEn,
            request.Slug,
            authorId,
            request.FeaturedImageUrl,
            _clock);

        await _service.SaveAsync(news, cancellationToken).ConfigureAwait(false);

        return ListNewsQueryHandler.MapToDto(news);
    }
}
