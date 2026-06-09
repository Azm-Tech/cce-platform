using CCE.Application.Common;
using CCE.Application.InterestManagement.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.InterestManagement.Commands.UpdateInterestTopic;

public sealed class UpdateInterestTopicCommandHandler
    : IRequestHandler<UpdateInterestTopicCommand, Response<InterestTopicDto>>
{
    private readonly IInterestTopicRepository _repo;
    private readonly MessageFactory _msg;

    public UpdateInterestTopicCommandHandler(IInterestTopicRepository repo, MessageFactory msg)
    {
        _repo = repo;
        _msg = msg;
    }

    public async Task<Response<InterestTopicDto>> Handle(
        UpdateInterestTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = await _repo.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (topic is null)
            return _msg.InterestTopicNotFound<InterestTopicDto>();

        topic.UpdateNames(request.NameAr, request.NameEn);
        await _repo.Update(topic).ConfigureAwait(false);
        return _msg.Ok(new InterestTopicDto(topic.Id, topic.NameAr, topic.NameEn, topic.IsActive), "INTEREST_TOPIC_UPDATED");
    }
}
