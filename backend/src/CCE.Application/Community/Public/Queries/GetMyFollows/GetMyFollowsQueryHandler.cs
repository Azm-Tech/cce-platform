using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetMyFollows;

public sealed class GetMyFollowsQueryHandler
    : IRequestHandler<GetMyFollowsQuery, Response<MyFollowsDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetMyFollowsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<MyFollowsDto>> Handle(
        GetMyFollowsQuery request,
        CancellationToken cancellationToken)
    {
        var topicIds = (await _db.TopicFollows
            .Where(f => f.UserId == request.UserId)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .Select(f => f.TopicId)
            .ToList();

        var userIds = (await _db.UserFollows
            .Where(f => f.FollowerId == request.UserId)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .Select(f => f.FollowedId)
            .ToList();

        var postIds = (await _db.PostFollows
            .Where(f => f.UserId == request.UserId)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .Select(f => f.PostId)
            .ToList();

        return _msg.Ok(new MyFollowsDto(topicIds, userIds, postIds), MessageKeys.General.ITEMS_LISTED);
    }
}
