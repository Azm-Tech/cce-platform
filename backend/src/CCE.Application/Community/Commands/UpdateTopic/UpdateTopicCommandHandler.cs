using CCE.Application.Community.Dtos;
using CCE.Application.Community.Queries.ListTopics;
using MediatR;

namespace CCE.Application.Community.Commands.UpdateTopic;

public sealed class UpdateTopicCommandHandler : IRequestHandler<UpdateTopicCommand, TopicDto?>
{
    private readonly ITopicService _service;

    public UpdateTopicCommandHandler(ITopicService service)
    {
        _service = service;
    }

    public async Task<TopicDto?> Handle(UpdateTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (topic is null)
        {
            return null;
        }

        topic.UpdateContent(request.NameAr, request.NameEn, request.DescriptionAr, request.DescriptionEn);
        topic.Reorder(request.OrderIndex);

        if (request.IsActive)
            topic.Activate();
        else
            topic.Deactivate();

        await _service.UpdateAsync(topic, cancellationToken).ConfigureAwait(false);

        return ListTopicsQueryHandler.MapToDto(topic);
    }
}
