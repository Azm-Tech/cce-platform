using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.DeleteTopic;

public sealed class DeleteTopicCommandHandler : IRequestHandler<DeleteTopicCommand, Unit>
{
    private readonly ITopicService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public DeleteTopicCommandHandler(ITopicService service, ICurrentUserAccessor currentUser, ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Unit> Handle(DeleteTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (topic is null)
        {
            throw new System.Collections.Generic.KeyNotFoundException($"Topic {request.Id} not found.");
        }

        var deletedById = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot delete topic from a request without a user identity.");

        topic.SoftDelete(deletedById, _clock);
        await _service.UpdateAsync(topic, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
