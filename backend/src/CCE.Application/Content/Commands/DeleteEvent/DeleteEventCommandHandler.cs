using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Content.Commands.DeleteEvent;

public sealed class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand, Unit>
{
    private readonly IEventService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public DeleteEventCommandHandler(IEventService service, ICurrentUserAccessor currentUser, ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Unit> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (ev is null)
        {
            throw new System.Collections.Generic.KeyNotFoundException($"Event {request.Id} not found.");
        }

        var deletedById = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot delete event from a request without a user identity.");

        ev.SoftDelete(deletedById, _clock);
        await _service.UpdateAsync(ev, ev.RowVersion, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
