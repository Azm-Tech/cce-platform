using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Application.Search;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Commands.RejectModerationRecord;

public sealed class RejectModerationRecordCommandHandler
    : IRequestHandler<RejectModerationRecordCommand, Response<VoidData>>
{
    private readonly ICceDbContext _db;
    private readonly ICommunityModerationService _service;
    private readonly IPostRepository _postRepo;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly ISearchClient _search;
    private readonly IRedisFeedStore _feedStore;
    private readonly MessageFactory _msg;

    public RejectModerationRecordCommandHandler(
        ICceDbContext db,
        ICommunityModerationService service,
        IPostRepository postRepo,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        ISearchClient search,
        IRedisFeedStore feedStore,
        MessageFactory msg)
    {
        _db       = db;
        _service  = service;
        _postRepo = postRepo;
        _currentUser = currentUser;
        _clock    = clock;
        _search   = search;
        _feedStore = feedStore;
        _msg      = msg;
    }

    public async Task<Response<VoidData>> Handle(
        RejectModerationRecordCommand request,
        CancellationToken cancellationToken)
    {
        var reviewerId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot reject moderation from a request without a user identity.");

        var existing = await _db.ModerationRecords
            .FirstOrDefaultAsync(r => r.Id == request.RecordId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
            throw new System.Collections.Generic.KeyNotFoundException($"ModerationRecord {request.RecordId} not found.");

        var humanRecord = ModerationRecord.CreateHuman(
            existing.ContentType, existing.ContentId,
            ModerationStatus.Rejected, request.Reason, reviewerId);
        _db.Add(humanRecord);

        if (existing.ContentType == ModerationContentType.Post)
        {
            var post = await _postRepo.GetIncludingDeletedAsync(existing.ContentId, cancellationToken)
                .ConfigureAwait(false);
            if (post is not null)
            {
                post.SetModerationStatus(ModerationStatus.Rejected);
                if (!post.IsDeleted)
                    post.SoftDelete(reviewerId, _clock);

                var author = await _db.Users
                    .FirstOrDefaultAsync(u => u.Id == post.AuthorId, cancellationToken)
                    .ConfigureAwait(false);
                author?.DecrementPostsCount();

                var community = await _db.Communities
                    .FirstOrDefaultAsync(c => c.Id == post.CommunityId, cancellationToken)
                    .ConfigureAwait(false);
                community?.DecrementPosts();

                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                await _search.DeleteAsync(SearchableType.CommunityPosts, post.Id, cancellationToken).ConfigureAwait(false);
                await _feedStore.RemovePostFromAllFeedsAsync(post.CommunityId, post.Id, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            var reply = await _service.FindReplyAsync(existing.ContentId, cancellationToken)
                .ConfigureAwait(false);
            if (reply is not null)
            {
                reply.SetModerationStatus(ModerationStatus.Rejected);
                if (!reply.IsDeleted)
                    reply.SoftDelete(reviewerId, _clock);

                var parentPost = await _postRepo.GetIncludingDeletedAsync(reply.PostId, cancellationToken)
                    .ConfigureAwait(false);
                parentPost?.DecrementCommentsCount(_clock);

                var author = await _db.Users
                    .FirstOrDefaultAsync(u => u.Id == reply.AuthorId, cancellationToken)
                    .ConfigureAwait(false);
                author?.DecrementCommentsCount();

                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                await _search.DeleteAsync(SearchableType.CommunityReplies, reply.Id, cancellationToken).ConfigureAwait(false);
            }
        }

        return _msg.Ok(MessageKeys.General.SUCCESS_UPDATED);
    }
}
