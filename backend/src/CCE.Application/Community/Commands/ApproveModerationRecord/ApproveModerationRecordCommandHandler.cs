using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Commands.ApproveModerationRecord;

public sealed class ApproveModerationRecordCommandHandler
    : IRequestHandler<ApproveModerationRecordCommand, Response<VoidData>>
{
    private readonly ICceDbContext _db;
    private readonly ICommunityModerationService _service;
    private readonly IPostRepository _postRepo;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public ApproveModerationRecordCommandHandler(
        ICceDbContext db,
        ICommunityModerationService service,
        IPostRepository postRepo,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory msg)
    {
        _db       = db;
        _service  = service;
        _postRepo = postRepo;
        _currentUser = currentUser;
        _clock    = clock;
        _msg      = msg;
    }

    public async Task<Response<VoidData>> Handle(
        ApproveModerationRecordCommand request,
        CancellationToken cancellationToken)
    {
        var reviewerId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot approve moderation from a request without a user identity.");

        var existing = await _db.ModerationRecords
            .FirstOrDefaultAsync(r => r.Id == request.RecordId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
            throw new System.Collections.Generic.KeyNotFoundException($"ModerationRecord {request.RecordId} not found.");

        var humanRecord = ModerationRecord.CreateHuman(
            existing.ContentType, existing.ContentId,
            ModerationStatus.Approved, reason: null, reviewerId);
        _db.Add(humanRecord);

        if (existing.ContentType == ModerationContentType.Post)
        {
            var post = await _postRepo.GetIncludingDeletedAsync(existing.ContentId, cancellationToken)
                .ConfigureAwait(false);
            if (post is not null)
            {
                post.SetModerationStatus(ModerationStatus.Approved);
                var wasDeleted = post.IsDeleted;
                if (wasDeleted)
                    post.Restore(reviewerId, _clock);

                if (wasDeleted)
                {
                    var author = await _db.Users
                        .FirstOrDefaultAsync(u => u.Id == post.AuthorId, cancellationToken)
                        .ConfigureAwait(false);
                    author?.IncrementPostsCount();

                    var community = await _db.Communities
                        .FirstOrDefaultAsync(c => c.Id == post.CommunityId, cancellationToken)
                        .ConfigureAwait(false);
                    community?.IncrementPosts();
                }

                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                await _service.ReIndexPostAsync(existing.ContentId, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            var reply = await _service.FindReplyAsync(existing.ContentId, cancellationToken)
                .ConfigureAwait(false);
            if (reply is not null)
            {
                reply.SetModerationStatus(ModerationStatus.Approved);
                var wasDeleted = reply.IsDeleted;
                if (wasDeleted)
                    reply.Restore(reviewerId, _clock);

                if (wasDeleted)
                {
                    var parentPost = await _postRepo.GetIncludingDeletedAsync(reply.PostId, cancellationToken)
                        .ConfigureAwait(false);
                    parentPost?.IncrementCommentsCount(_clock);

                    var author = await _db.Users
                        .FirstOrDefaultAsync(u => u.Id == reply.AuthorId, cancellationToken)
                        .ConfigureAwait(false);
                    author?.IncrementCommentsCount();
                }

                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                await _service.ReIndexReplyAsync(existing.ContentId, cancellationToken).ConfigureAwait(false);
            }
        }

        return _msg.Ok(MessageKeys.General.SUCCESS_UPDATED);
    }
}
