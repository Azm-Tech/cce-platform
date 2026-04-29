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
            .Where(p => p.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .FirstOrDefault();

        return post is null ? null : ListPublicPostsInTopicQueryHandler.MapToDto(post);
    }
}
