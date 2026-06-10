using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Content.Commands.UnfollowNews;

internal sealed class UnfollowNewsCommandHandler(
    IRepository<NewsFollow, System.Guid> _repo,
    ICceDbContext _db,
    ICurrentUserAccessor _currentUser,
    MessageFactory _msg)
    : IRequestHandler<UnfollowNewsCommand, Response<VoidData>>
{
    public async Task<Response<VoidData>> Handle(UnfollowNewsCommand request, CancellationToken ct)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null)
            return _msg.Unauthorized<VoidData>("NOT_AUTHENTICATED");

        var follow = await _db.NewsFollows
            .FirstOrDefaultAsync(f => f.UserId == userId.Value, ct)
            .ConfigureAwait(false);

        if (follow is null)
            return _msg.NotFound<VoidData>("NEWS_FOLLOW_NOT_FOUND");

        _repo.Delete(follow);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return _msg.Ok("NEWS_UNFOLLOWED");
    }
}
