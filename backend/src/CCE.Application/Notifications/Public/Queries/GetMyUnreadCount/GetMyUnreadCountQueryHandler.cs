using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Public.Queries.GetMyUnreadCount;

public sealed class GetMyUnreadCountQueryHandler : IRequestHandler<GetMyUnreadCountQuery, Response<int>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetMyUnreadCountQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<int>> Handle(GetMyUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        var count = await _db.UserNotifications
            .Where(n => n.UserId == userId && n.Status == NotificationStatus.Sent)
            .CountAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        return _msg.Ok(count, MessageKeys.General.ITEMS_LISTED);
    }
}
