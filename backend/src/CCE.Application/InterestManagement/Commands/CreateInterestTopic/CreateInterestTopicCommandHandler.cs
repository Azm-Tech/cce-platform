using CCE.Application.Common;
using CCE.Application.InterestManagement.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.InterestManagement.Commands.CreateInterestTopic;

public sealed class CreateInterestTopicCommandHandler
    : IRequestHandler<CreateInterestTopicCommand, Response<InterestTopicDto>>
{
    private readonly IInterestTopicRepository _repo;
    private readonly MessageFactory _msg;

    public CreateInterestTopicCommandHandler(IInterestTopicRepository repo, MessageFactory msg)
    {
        _repo = repo;
        _msg = msg;
    }

    public async Task<Response<InterestTopicDto>> Handle(
        CreateInterestTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = InterestTopic.Create(request.NameAr, request.NameEn);
        await _repo.AddAsync(topic, cancellationToken).ConfigureAwait(false);
        return _msg.Ok(new InterestTopicDto(topic.Id, topic.NameAr, topic.NameEn, topic.IsActive), "INTEREST_TOPIC_CREATED");
    }
}
