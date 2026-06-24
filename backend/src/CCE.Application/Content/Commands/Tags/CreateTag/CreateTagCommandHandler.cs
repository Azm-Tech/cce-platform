using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.Tags.CreateTag;

public sealed class CreateTagCommandHandler : IRequestHandler<CreateTagCommand, Response<TagDto>>
{
    private readonly IRepository<Tag, System.Guid> _repo;
    private readonly MessageFactory _messages;

    public CreateTagCommandHandler(IRepository<Tag, System.Guid> repo, MessageFactory messages)
    {
        _repo = repo;
        _messages = messages;
    }

    public async Task<Response<TagDto>> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        var tag = Tag.Create(request.NameAr, request.NameEn, request.Color);
        await _repo.AddAsync(tag, cancellationToken).ConfigureAwait(false);
        return _messages.Ok(new TagDto(tag.Id, tag.NameAr, tag.NameEn, tag.Color), MessageKeys.Content.CONTENT_CREATED);
    }
}
