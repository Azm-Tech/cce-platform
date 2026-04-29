using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Public.Queries.GetMyUnreadCount;

public sealed class GetMyUnreadCountQueryHandler : IRequestHandler<GetMyUnreadCountQuery, int>
{
    private readonly ICceDbContext _db;

    public GetMyUnreadCountQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<int> Handle(GetMyUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        return await _db.UserNotifications
            .Where(n => n.UserId == userId && n.Status == NotificationStatus.Sent)
            .CountAsyncEither(cancellationToken)
            .ConfigureAwait(false);
    }
}
