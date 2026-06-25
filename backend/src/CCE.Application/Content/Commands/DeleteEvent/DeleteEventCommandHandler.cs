using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.DeleteEvent;

public sealed class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand, Response<VoidData>>
{
    private readonly IRepository<Event, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;

    public DeleteEventCommandHandler(
        IRepository<Event, System.Guid> repo,
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _messages = messages;
    }

    public async Task<Response<VoidData>> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (ev is null)
            return _messages.NotFound<VoidData>(MessageKeys.Content.EVENT_NOT_FOUND);

        var userId = _currentUser.GetUserId();
        if (userId is null)
            return _messages.Unauthorized<VoidData>(MessageKeys.Identity.NOT_AUTHENTICATED);

        ev.SoftDelete(userId.Value, _clock);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _messages.Ok(MessageKeys.Content.CONTENT_DELETED);
    }
}
