using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.InterestManagement.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.InterestManagement.Queries.GetInterestTopicById;

public sealed class GetInterestTopicByIdQueryHandler
    : IRequestHandler<GetInterestTopicByIdQuery, Response<InterestTopicDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetInterestTopicByIdQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<InterestTopicDto>> Handle(
        GetInterestTopicByIdQuery request, CancellationToken cancellationToken)
    {
        var topics = await _db.InterestTopics
            .Where(t => t.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var topic = topics.SingleOrDefault();

        if (topic is null)
            return _msg.NotFound<InterestTopicDto>("INTEREST_TOPIC_NOT_FOUND");

        return _msg.Ok(new InterestTopicDto(topic.Id, topic.NameAr, topic.NameEn, topic.IsActive), "SUCCESS_OPERATION");
    }
}
