using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Dtos;
using CCE.Application.Community.Queries.ListTopics;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Community.Queries.GetTopicById;

public sealed class GetTopicByIdQueryHandler : IRequestHandler<GetTopicByIdQuery, Response<TopicDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public GetTopicByIdQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<TopicDto>> Handle(GetTopicByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Topics
            .Where(t => t.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var topic = list.SingleOrDefault();
        if (topic is null)
            return _messages.TopicNotFound<TopicDto>();

        return _messages.Ok(ListTopicsQueryHandler.MapToDto(topic), "SUCCESS_OPERATION");
    }
}
