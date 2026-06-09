using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Community.Public.Queries.ListPublicPostsInTopic;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetPublicPostById;

public sealed class GetPublicPostByIdQueryHandler
    : IRequestHandler<GetPublicPostByIdQuery, PublicPostDto?>
{
    private readonly ICceDbContext _db;

    public GetPublicPostByIdQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PublicPostDto?> Handle(
        GetPublicPostByIdQuery request,
        CancellationToken cancellationToken)
    {
        var post = (await _db.Posts
            .Where(p => p.Id == request.Id && p.Status == CCE.Domain.Community.PostStatus.Published)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .FirstOrDefault();

        if (post is null)
            return null;

        var authorName = (await _db.Users
            .Where(u => u.Id == post.AuthorId)
            .Select(u => u.FirstName + " " + u.LastName)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .FirstOrDefault();

        var attachments = (await _db.PostAttachments
            .Where(a => a.PostId == post.Id)
            .Select(a => a.AssetFileId)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .ToList();

        return ListPublicPostsInTopicQueryHandler.MapToDto(post, authorName, attachments);
    }
}
