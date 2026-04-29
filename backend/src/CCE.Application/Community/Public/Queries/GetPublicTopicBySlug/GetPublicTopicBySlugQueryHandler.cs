using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Community.Public.Queries.ListPublicTopics;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetPublicTopicBySlug;

public sealed class GetPublicTopicBySlugQueryHandler
    : IRequestHandler<GetPublicTopicBySlugQuery, PublicTopicDto?>
{
    private readonly ICceDbContext _db;

    public GetPublicTopicBySlugQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PublicTopicDto?> Handle(
        GetPublicTopicBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var topic = (await _db.Topics
            .Where(t => t.IsActive && t.Slug == request.Slug)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .FirstOrDefault();

        return topic is null ? null : ListPublicTopicsQueryHandler.MapToDto(topic);
    }
}
