using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Content.Public.Queries.ListPublicResources;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicResourceById;

public sealed class GetPublicResourceByIdQueryHandler : IRequestHandler<GetPublicResourceByIdQuery, PublicResourceDto?>
{
    private readonly ICceDbContext _db;

    public GetPublicResourceByIdQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PublicResourceDto?> Handle(GetPublicResourceByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Resources
            .Where(r => r.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var resource = list.SingleOrDefault();
        if (resource is null || resource.PublishedOn is null)
        {
            return null;
        }

        return ListPublicResourcesQueryHandler.MapToDto(resource);
    }
}
