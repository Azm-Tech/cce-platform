using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.Tags.UpdateTag;

public sealed class UpdateTagCommandHandler : IRequestHandler<UpdateTagCommand, Response<TagDto>>
{
    private readonly IRepository<Tag, System.Guid> _repo;
    private readonly MessageFactory _messages;

    public UpdateTagCommandHandler(IRepository<Tag, System.Guid> repo, MessageFactory messages)
    {
        _repo = repo;
        _messages = messages;
    }

    public async Task<Response<TagDto>> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (tag is null)
            return _messages.NotFound<TagDto>(MessageKeys.Content.TAG_NOT_FOUND);

        tag.Update(request.NameAr, request.NameEn, request.Color);
        return _messages.Ok(new TagDto(tag.Id, tag.NameAr, tag.NameEn, tag.Color), MessageKeys.General.SUCCESS_OPERATION);
    }
}
