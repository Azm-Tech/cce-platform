using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Commands.RebuildHotLeaderboard;

internal sealed class RebuildHotLeaderboardCommandHandler
    : IRequestHandler<RebuildHotLeaderboardCommand, Response<VoidData>>
{
    private readonly ICceDbContext _db;
    private readonly IRedisFeedStore _feedStore;
    private readonly MessageFactory _msg;

    public RebuildHotLeaderboardCommandHandler(
        ICceDbContext db,
        IRedisFeedStore feedStore,
        MessageFactory msg)
    {
        _db = db;
        _feedStore = feedStore;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(
        RebuildHotLeaderboardCommand request, CancellationToken cancellationToken)
    {
        var communityIds = request.CommunityId.HasValue
            ? [request.CommunityId.Value]
            : await _db.Communities
                .AsNoTracking()
                .Select(c => c.Id)
                .ToListAsyncEither(cancellationToken)
                .ConfigureAwait(false);

        foreach (var communityId in communityIds)
        {
            var posts = await _db.Posts
                .AsNoTracking()
                .Where(p => p.CommunityId == communityId && p.Status == PostStatus.Published)
                .OrderByDescending(p => p.Score)
                .Take(1000)
                .Select(p => new { p.Id, p.Score })
                .ToListAsyncEither(cancellationToken)
                .ConfigureAwait(false);

            foreach (var post in posts)
            {
                await _feedStore.AddToHotLeaderboardAsync(communityId, post.Id, post.Score, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return _msg.Ok("SUCCESS_OPERATION");
    }
}
