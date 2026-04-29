using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.MarkPostAnswered;

public sealed class MarkPostAnsweredCommandHandler : IRequestHandler<MarkPostAnsweredCommand, Unit>
{
    private readonly ICommunityWriteService _service;
    private readonly ICurrentUserAccessor _currentUser;

    public MarkPostAnsweredCommandHandler(
        ICommunityWriteService service,
        ICurrentUserAccessor currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(MarkPostAnsweredCommand request, CancellationToken cancellationToken)
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
        return Unit.Value;
    }
}
