using CCE.Application.Common;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Common.Interfaces;
using CCE.Application.Errors;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Public.Queries.GetPublicPostById;

public sealed class GetPublicPostByIdQueryHandler
    : IRequestHandler<GetPublicPostByIdQuery, Response<PostDetailDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetPublicPostByIdQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PostDetailDto>> Handle(
        GetPublicPostByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = request.UserId;

        var dto = await (
            from p in _db.Posts.AsNoTracking()
            join u in _db.Users.AsNoTracking() on p.AuthorId equals u.Id
            join t in _db.Topics.AsNoTracking() on p.TopicId equals t.Id
            where p.Id == request.Id && p.Status == PostStatus.Published
            select new PostDetailDto(
                p.Id,
                p.CommunityId,
                p.TopicId,
                new PostAuthorDto(
                    u.Id,
                    u.FirstName + " " + u.LastName,
                    u.AvatarUrl,
                    _db.ExpertProfiles.Any(e => e.UserId == u.Id),
                    u.PostsCount,
                    u.FollowerCount),
                p.Type,
                p.Title,
                p.Content,
                p.Locale,
                p.IsAnswerable,
                p.AnsweredReplyId,
                p.UpvoteCount,
                p.DownvoteCount,
                p.CommentsCount,
                p.Attachments.Select(a => a.AssetFileId).ToList(),
                p.CreatedOn,
                t.NameAr,
                t.NameEn,
                userId.HasValue && _db.PostFollows.Any(pf =>
                    pf.PostId == p.Id && pf.UserId == userId.Value),
                userId.HasValue
                    ? _db.PostVotes
                        .Where(pv => pv.PostId == p.Id && pv.UserId == userId.Value)
                        .Select(pv => pv.Value)
                        .FirstOrDefault()
                    : 0))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return dto is null
            ? _msg.NotFound<PostDetailDto>(ApplicationErrors.Community.POST_NOT_FOUND)
            : _msg.Ok(dto, ApplicationErrors.General.SUCCESS_OPERATION);
    }
}
