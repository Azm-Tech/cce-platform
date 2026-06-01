using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.GetNewsById;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.UpdateNews;

public sealed class UpdateNewsCommandHandler : IRequestHandler<UpdateNewsCommand, Response<NewsDto>>
{
    private readonly IRepository<News, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public UpdateNewsCommandHandler(
        IRepository<News, System.Guid> repo,
        ICceDbContext db,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _messages = messages;
    }

    public async Task<Response<NewsDto>> Handle(UpdateNewsCommand request, CancellationToken cancellationToken)
    {
        var news = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (news is null)
            return _messages.NewsNotFound<NewsDto>();

        var topicExists = await _db.Topics.Where(t => t.Id == request.TopicId).CountAsyncEither(cancellationToken) > 0;
        if (!topicExists)
            return _messages.NotFound<NewsDto>("TOPIC_NOT_FOUND");

        var expectedRowVersion = news.RowVersion;
        news.UpdateContent(
            request.TitleAr,
            request.TitleEn,
            request.ContentAr,
            request.ContentEn,
            request.TopicId,
            request.FeaturedImageUrl);

        _db.SetExpectedRowVersion(news, expectedRowVersion);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var topic = await _db.Topics.Where(t => t.Id == request.TopicId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topicNameAr = topic.FirstOrDefault()?.NameAr ?? string.Empty;
        var topicNameEn = topic.FirstOrDefault()?.NameEn ?? string.Empty;

        return _messages.Ok(GetNewsByIdQueryHandler.MapToDto(news, topicNameAr, topicNameEn), "SUCCESS_OPERATION");
    }
}
