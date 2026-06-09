using CCE.Application.Common;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.InterestManagement.Commands.DeleteInterestTopic;

public sealed class DeleteInterestTopicCommandHandler
    : IRequestHandler<DeleteInterestTopicCommand, Response<VoidData>>
{
    private readonly IInterestTopicRepository _repo;
    private readonly MessageFactory _msg;

    public DeleteInterestTopicCommandHandler(IInterestTopicRepository repo, MessageFactory msg)
    {
        _repo = repo;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(
        DeleteInterestTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = await _repo.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (topic is null)
            return _msg.NotFound<VoidData>("INTEREST_TOPIC_NOT_FOUND");

        await _repo.Delete(topic).ConfigureAwait(false);
        return _msg.Ok("INTEREST_TOPIC_DELETED");
    }
}
