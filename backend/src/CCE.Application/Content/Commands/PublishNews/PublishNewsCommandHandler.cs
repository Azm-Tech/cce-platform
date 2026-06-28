using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.GetNewsById;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.PublishNews;

public sealed class PublishNewsCommandHandler : IRequestHandler<PublishNewsCommand, Response<NewsDto>>
{
    private readonly IRepository<News, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;

    public PublishNewsCommandHandler(
        IRepository<News, System.Guid> repo,
        ICceDbContext db,
        ISystemClock clock,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _clock = clock;
        _messages = messages;
    }

    public async Task<Response<NewsDto>> Handle(PublishNewsCommand request, CancellationToken cancellationToken)
    {
        var news = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (news is null)
            return _messages.NotFound<NewsDto>(MessageKeys.Content.NEWS_NOT_FOUND);

        var expectedRowVersion = news.RowVersion;
        news.Publish(_clock);

        _db.SetExpectedRowVersion(news, expectedRowVersion);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _messages.Ok(GetNewsByIdQueryHandler.MapToDto(news), MessageKeys.General.SUCCESS_OPERATION);
    }
}
