using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Community.Public.Queries.ListPublicTopics;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetPublicTopicBySlug;

public sealed class GetPublicTopicBySlugQueryHandler
    : IRequestHandler<GetPublicTopicBySlugQuery, Response<PublicTopicDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public GetPublicTopicBySlugQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<PublicTopicDto>> Handle(
        GetPublicTopicBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var topic = (await _db.Topics
            .Where(t => t.IsActive && t.Slug == request.Slug)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .FirstOrDefault();

        if (topic is null)
            return _messages.TopicNotFound<PublicTopicDto>();

        return _messages.Ok(ListPublicTopicsQueryHandler.MapToDto(topic), MessageKeys.General.SUCCESS_OPERATION);
    }
}
