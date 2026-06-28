using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Dtos;
using CCE.Application.Community.Queries.ListTopics;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.CreateTopic;

public sealed class CreateTopicCommandHandler : IRequestHandler<CreateTopicCommand, Response<TopicDto>>
{
    private readonly IRepository<Topic, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public CreateTopicCommandHandler(
        IRepository<Topic, System.Guid> repo,
        ICceDbContext db,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _messages = messages;
    }

    public async Task<Response<TopicDto>> Handle(CreateTopicCommand request, CancellationToken cancellationToken)
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

        await _repo.AddAsync(topic, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _messages.Ok(ListTopicsQueryHandler.MapToDto(topic), MessageKeys.Content.CONTENT_CREATED);
    }
}
