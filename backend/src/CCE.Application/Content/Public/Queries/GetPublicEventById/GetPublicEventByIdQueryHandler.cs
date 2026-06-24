using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicEventById;

public sealed class GetPublicEventByIdQueryHandler : IRequestHandler<GetPublicEventByIdQuery, Response<PublicEventDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public GetPublicEventByIdQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<PublicEventDto>> Handle(GetPublicEventByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Events
            .Where(e => e.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var ev = list.SingleOrDefault();
        if (ev is null)
            return _messages.EventNotFound<PublicEventDto>();

        var topics = await _db.Topics.Where(t => t.Id == ev.TopicId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topic = topics.FirstOrDefault();

        var tagDtos = ev.Tags.Select(t => new TagDto(t.Id, t.NameAr, t.NameEn, t.Color)).ToList();

        return _messages.Ok(MapToDto(ev, topic?.NameAr ?? string.Empty, topic?.NameEn ?? string.Empty, tagDtos), MessageKeys.General.SUCCESS_OPERATION);
    }

    internal static PublicEventDto MapToDto(Event e, string topicNameAr, string topicNameEn, System.Collections.Generic.IReadOnlyList<TagDto>? tags = null) => new(
        e.Id,
        e.TitleAr,
        e.TitleEn,
        e.DescriptionAr,
        e.DescriptionEn,
        e.StartsOn,
        e.EndsOn,
        e.LocationAr,
        e.LocationEn,
        e.OnlineMeetingUrl,
        e.FeaturedImageUrl,
        e.ICalUid,
        e.TopicId,
        topicNameAr,
        topicNameEn,
        tags ?? new List<TagDto>(),
        e.KnowledgeLevelId,
        e.JobSectorId);
}
