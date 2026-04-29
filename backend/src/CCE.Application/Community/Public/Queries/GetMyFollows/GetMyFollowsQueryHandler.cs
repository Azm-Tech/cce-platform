using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetMyFollows;

public sealed class GetMyFollowsQueryHandler
    : IRequestHandler<GetMyFollowsQuery, MyFollowsDto>
{
    private readonly ICceDbContext _db;

    public GetMyFollowsQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<MyFollowsDto> Handle(
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

        return new MyFollowsDto(topicIds, userIds, postIds);
    }
}
