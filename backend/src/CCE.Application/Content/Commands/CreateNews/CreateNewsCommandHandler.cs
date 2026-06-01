using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListNews;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.CreateNews;

public sealed class CreateNewsCommandHandler : IRequestHandler<CreateNewsCommand, Response<NewsDto>>
{
    private readonly IRepository<News, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;

    public CreateNewsCommandHandler(
        IRepository<News, System.Guid> repo,
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _messages = messages;
    }

    public async Task<Response<NewsDto>> Handle(CreateNewsCommand request, CancellationToken cancellationToken)
    {
        var authorId = _currentUser.GetUserId();
        if (authorId is null)
            return _messages.NotAuthenticated<NewsDto>();

        var topicExists = await _db.Topics.Where(t => t.Id == request.TopicId).CountAsyncEither(cancellationToken) > 0;
        if (!topicExists)
            return _messages.NotFound<NewsDto>("TOPIC_NOT_FOUND");

        var news = News.Draft(
            request.TitleAr,
            request.TitleEn,
            request.ContentAr,
            request.ContentEn,
            request.TopicId,
            authorId.Value,
            request.FeaturedImageUrl,
            _clock);

        await _repo.AddAsync(news, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var topic = await _db.Topics.Where(t => t.Id == request.TopicId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topicNameAr = topic.FirstOrDefault()?.NameAr ?? string.Empty;
        var topicNameEn = topic.FirstOrDefault()?.NameEn ?? string.Empty;

        return _messages.Ok(ListNewsQueryHandler.MapToDto(news, topicNameAr, topicNameEn), "CONTENT_CREATED");
    }
}
