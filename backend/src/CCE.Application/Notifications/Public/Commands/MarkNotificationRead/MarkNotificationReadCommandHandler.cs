using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Community;
using CCE.Application.Messages;
using CCE.Application.Notifications.Public;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Notifications.Public.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Response<VoidData>>
{
    private readonly IUserNotificationRepository _repo;
    private readonly ICceDbContext _db;
    private readonly IRedisFeedStore _feedStore;
    private readonly MessageFactory _msg;
    private readonly ISystemClock _clock;

    public MarkNotificationReadCommandHandler(
        IUserNotificationRepository repo,
        ICceDbContext db,
        IRedisFeedStore feedStore,
        MessageFactory msg,
        ISystemClock clock)
    {
        _repo = repo;
        _db = db;
        _feedStore = feedStore;
        _msg = msg;
        _clock = clock;
    }

    public async Task<Response<VoidData>> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notif = await _repo.GetAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (notif is null || notif.UserId != request.UserId)
            return _msg.NotificationLogNotFound<VoidData>();

        notif.MarkRead(_clock);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Decrement the badge counter by 1. RedisFeedStore clamps at 0 so this is safe
        // even if the counter is already stale or missing.
        await _feedStore.IncrementNotificationCountAsync(notif.UserId, delta: -1, cancellationToken)
            .ConfigureAwait(false);

        return _msg.NotificationMarkedRead();
    }
}
