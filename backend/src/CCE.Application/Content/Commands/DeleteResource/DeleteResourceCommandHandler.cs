using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.DeleteResource;

public sealed class DeleteResourceCommandHandler : IRequestHandler<DeleteResourceCommand, Response<VoidData>>
{
    private readonly IRepository<Resource, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;

    public DeleteResourceCommandHandler(
        IRepository<Resource, System.Guid> repo,
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

    public async Task<Response<VoidData>> Handle(DeleteResourceCommand request, CancellationToken cancellationToken)
    {
        var resource = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (resource is null)
            return _messages.ResourceNotFound<VoidData>();

        var userId = _currentUser.GetUserId();
        if (userId is null)
            return _messages.NotAuthenticated<VoidData>();

        resource.SoftDelete(userId.Value, _clock);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _messages.Ok(MessageKeys.Content.RESOURCE_DELETED);
    }
}
