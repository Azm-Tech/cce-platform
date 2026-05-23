using CCE.Application.Common;
using CCE.Application.Messages;
using CCE.Application.Notifications.Public;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Notifications.Public.Commands.MarkAllNotificationsRead;

public sealed class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, Response<int>>
{
    private readonly IUserNotificationRepository _repo;
    private readonly MessageFactory _msg;
    private readonly ISystemClock _clock;

    public MarkAllNotificationsReadCommandHandler(
        IUserNotificationRepository repo,
        MessageFactory msg,
        ISystemClock clock)
    {
        _repo = repo;
        _msg = msg;
        _clock = clock;
    }

    public async Task<Response<int>> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var count = await _repo.MarkAllSentAsReadAsync(
            request.UserId,
            _clock,
            cancellationToken).ConfigureAwait(false);
        return _msg.NotificationsMarkedRead(count);
    }
}
