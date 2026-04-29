using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Dtos;
using CCE.Application.Community.Queries.ListTopics;
using MediatR;

namespace CCE.Application.Community.Queries.GetTopicById;

public sealed class GetTopicByIdQueryHandler : IRequestHandler<GetTopicByIdQuery, TopicDto?>
{
    private readonly ICceDbContext _db;

    public GetTopicByIdQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<TopicDto?> Handle(GetTopicByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Topics
            .Where(t => t.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var topic = list.SingleOrDefault();
        return topic is null ? null : ListTopicsQueryHandler.MapToDto(topic);
    }
}
