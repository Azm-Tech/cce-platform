using MediatR;

namespace CCE.Application.Notifications.Public.Commands.MarkAllNotificationsRead;

public sealed class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    private readonly IUserNotificationService _service;

    public MarkAllNotificationsReadCommandHandler(IUserNotificationService service)
    {
        _service = service;
    }

    public async Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        return await _service.MarkAllSentAsReadAsync(request.UserId, cancellationToken).ConfigureAwait(false);
    }
}
