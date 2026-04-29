using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Notifications.Public.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Unit>
{
    private readonly IUserNotificationService _service;
    private readonly ISystemClock _clock;

    public MarkNotificationReadCommandHandler(IUserNotificationService service, ISystemClock clock)
    {
        _service = service;
        _clock = clock;
    }

    public async Task<Unit> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notif = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (notif is null || notif.UserId != request.UserId)
            throw new KeyNotFoundException($"Notification {request.Id} not found.");

        notif.MarkRead(_clock);
        await _service.UpdateAsync(notif, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
