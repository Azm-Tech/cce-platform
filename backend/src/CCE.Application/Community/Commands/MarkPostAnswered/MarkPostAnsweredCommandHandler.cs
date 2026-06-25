using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.MarkPostAnswered;

public sealed class MarkPostAnsweredCommandHandler : IRequestHandler<MarkPostAnsweredCommand, Response<VoidData>>
{
    private readonly ICommunityWriteService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly MessageFactory _msg;

    public MarkPostAnsweredCommandHandler(
        ICommunityWriteService service,
        ICurrentUserAccessor currentUser,
        MessageFactory msg)
    {
        _service = service;
        _currentUser = currentUser;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(MarkPostAnsweredCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot mark answer without a user identity.");

        var post = await _service.FindPostAsync(request.PostId, cancellationToken).ConfigureAwait(false);
        if (post is null)
        {
            throw new KeyNotFoundException($"Post {request.PostId} not found.");
        }

        if (post.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("Only the post author may mark a reply as the answer.");
        }

        // Verify the reply belongs to this post
        var reply = await _service.FindReplyAsync(request.ReplyId, cancellationToken).ConfigureAwait(false);
        if (reply is null || reply.PostId != request.PostId)
        {
            throw new KeyNotFoundException($"Reply {request.ReplyId} not found on post {request.PostId}.");
        }

        post.MarkAnswered(request.ReplyId);
        await _service.UpdatePostAsync(post, cancellationToken).ConfigureAwait(false);
        return _msg.Ok(MessageKeys.General.SUCCESS_OPERATION);
    }
}
