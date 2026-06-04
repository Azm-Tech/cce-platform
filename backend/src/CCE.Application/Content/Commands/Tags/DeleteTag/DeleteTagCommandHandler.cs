using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.Tags.DeleteTag;

public sealed class DeleteTagCommandHandler : IRequestHandler<DeleteTagCommand, Response<VoidData>>
{
    private readonly IRepository<Tag, System.Guid> _repo;
    private readonly MessageFactory _messages;

    public DeleteTagCommandHandler(IRepository<Tag, System.Guid> repo, MessageFactory messages)
    {
        _repo = repo;
        _messages = messages;
    }

    public async Task<Response<VoidData>> Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (tag is null)
            return _messages.NotFound<VoidData>("TAG_NOT_FOUND");

        _repo.Delete(tag);
        return _messages.Ok(VoidData.Instance, "SUCCESS_OPERATION");
    }
}
