using CCE.Application.Community.Dtos;
using CCE.Application.Community.Queries.ListTopics;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.CreateTopic;

public sealed class CreateTopicCommandHandler : IRequestHandler<CreateTopicCommand, TopicDto>
{
    private readonly ITopicService _service;

    public CreateTopicCommandHandler(ITopicService service)
    {
        _service = service;
    }

    public async Task<TopicDto> Handle(CreateTopicCommand request, CancellationToken cancellationToken)
    {
        var topic = Topic.Create(
            request.NameAr,
            request.NameEn,
            request.DescriptionAr,
            request.DescriptionEn,
            request.Slug,
            request.ParentId,
            request.IconUrl,
            request.OrderIndex);

        await _service.SaveAsync(topic, cancellationToken).ConfigureAwait(false);

        return ListTopicsQueryHandler.MapToDto(topic);
    }
}
