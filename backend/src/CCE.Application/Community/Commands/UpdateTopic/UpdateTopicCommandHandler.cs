using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Dtos;
using CCE.Application.Community.Queries.ListTopics;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.UpdateTopic;

public sealed class UpdateTopicCommandHandler : IRequestHandler<UpdateTopicCommand, Response<TopicDto>>
{
    private readonly IRepository<Topic, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public UpdateTopicCommandHandler(
        IRepository<Topic, System.Guid> repo,
        ICceDbContext db,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _messages = messages;
    }

    public async Task<Response<TopicDto>> Handle(UpdateTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (topic is null)
            return _messages.NotFound<TopicDto>(MessageKeys.Community.TOPIC_NOT_FOUND);

        topic.UpdateContent(request.NameAr, request.NameEn, request.DescriptionAr, request.DescriptionEn);
        topic.Reorder(request.OrderIndex);

        if (request.IsActive)
            topic.Activate();
        else
            topic.Deactivate();

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _messages.Ok(ListTopicsQueryHandler.MapToDto(topic), MessageKeys.General.SUCCESS_OPERATION);
    }
}
