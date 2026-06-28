using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicHomepageSections;

public sealed class ListPublicHomepageSectionsQueryHandler
    : IRequestHandler<ListPublicHomepageSectionsQuery, Response<System.Collections.Generic.IReadOnlyList<PublicHomepageSectionDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListPublicHomepageSectionsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<System.Collections.Generic.IReadOnlyList<PublicHomepageSectionDto>>> Handle(
        ListPublicHomepageSectionsQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _db.HomepageSections
            .Where(s => s.IsActive)
            .OrderBy(s => s.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        return _msg.Ok((System.Collections.Generic.IReadOnlyList<PublicHomepageSectionDto>)list.Select(MapToDto).ToList(), MessageKeys.General.ITEMS_LISTED);
    }

    internal static PublicHomepageSectionDto MapToDto(HomepageSection s) => new(
        s.Id,
        s.SectionType,
        s.OrderIndex,
        s.ContentAr,
        s.ContentEn);
}
