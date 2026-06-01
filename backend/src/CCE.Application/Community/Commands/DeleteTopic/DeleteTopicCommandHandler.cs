using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.DeleteTopic;

public sealed class DeleteTopicCommandHandler : IRequestHandler<DeleteTopicCommand, Response<VoidData>>
{
    private readonly IRepository<Topic, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;

    public DeleteTopicCommandHandler(
        IRepository<Topic, System.Guid> repo,
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

    public async Task<Response<VoidData>> Handle(DeleteTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (topic is null)
            return _messages.TopicNotFound<VoidData>();

        var deletedById = _currentUser.GetUserId();
        if (deletedById is null)
            return _messages.NotAuthenticated<VoidData>();

        topic.SoftDelete(deletedById.Value, _clock);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _messages.Ok("CONTENT_DELETED");
    }
}
