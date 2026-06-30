using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.GetNewsById;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        var news = await _repo.GetByIdAsync(
            request.Id,
            q => q.Include(n => n.Tags),
            cancellationToken).ConfigureAwait(false);
        if (news is null)
            return _messages.NotFound<NewsDto>(MessageKeys.Content.NEWS_NOT_FOUND);

        var topicExists = await _db.Topics.Where(t => t.Id == request.TopicId).CountAsyncEither(cancellationToken) > 0;
        if (!topicExists)
            return _messages.NotFound<NewsDto>(MessageKeys.Community.TOPIC_NOT_FOUND);

        var expectedRowVersion = news.RowVersion;
        news.UpdateContent(
            request.TitleAr,
            request.TitleEn,
            request.ContentAr,
            request.ContentEn,
            request.TopicId,
            request.FeaturedImageUrl,
            request.KnowledgeLevelId,
            request.JobSectorId);

        if (request.TagIds is not null)
        {
            var requested = await _db.Tags.Where(t => request.TagIds.Contains(t.Id))
                .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
            // Tags load detached (ICceDbContext is AsNoTracking). Reuse the instances already tracked
            // via Include(n => n.Tags); attach the genuinely-new ones as Unchanged. This avoids both
            // the pk_tags INSERT (PK violation) and the "instance already tracked" error on re-link.
            var current = news.Tags.ToDictionary(t => t.Id);
            var resolved = new System.Collections.Generic.List<Tag>(requested.Count);
            foreach (var tag in requested)
            {
                if (current.TryGetValue(tag.Id, out var tracked)) { resolved.Add(tracked); }
                else { _db.Attach(tag); resolved.Add(tag); }
            }
            news.SetTags(resolved);
        }

        _db.SetExpectedRowVersion(news, expectedRowVersion);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var topic = await _db.Topics.Where(t => t.Id == request.TopicId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topicNameAr = topic.FirstOrDefault()?.NameAr ?? string.Empty;
        var topicNameEn = topic.FirstOrDefault()?.NameEn ?? string.Empty;

        var tagDtos = news.Tags.Select(t => new TagDto(t.Id, t.NameAr, t.NameEn, t.Color)).ToList();
        return _messages.Ok(GetNewsByIdQueryHandler.MapToDto(news, topicNameAr, topicNameEn, tagDtos), MessageKeys.General.SUCCESS_OPERATION);
    }
}
